using System;
using S1API.Entities;

using NativeNpc = Il2CppScheduleOne.NPCs.NPC;

namespace CustomNPCExample.Casino
{
    /// <summary>
    /// The casino conversation: Philip Wentworth at the cage takes the player's cash, holds it for
    /// a few in-game days, and pays it into the bank when the player comes back to claim it.
    ///
    /// Nothing replaces Philip's own dialogue: the nodes are built through S1API into this
    /// feature's own container, and a single choice ("I need to move some money through the
    /// cage.") is attached to Philip's own menu through the game's
    /// <see cref="Utils.WvcDialogueChoice"/> helper - his vanilla options (buy and the rest) stay
    /// exactly where they were, and the choice exists on his controller alone. The container and
    /// choice are rebuilt before every opening and after every transaction, so the countdown and
    /// shortfall wording is never stale.
    ///
    /// The confirm choices are the only ones with callbacks: both re-validate against the live
    /// state and run the move. If a move is refused, the conversation is closed rather than
    /// letting the success line stand next to unchanged balances.
    /// </summary>
    public static class CasinoLaunderingDialogue
    {
        public const string ContainerName = "wvc_philip_casino";

        public const string MenuChoice = "philip_launder_menu";
        public const string MenuChoiceText = "I need to move some money through the cage.";

        // Node labels. Deliberately prefixed: they live in the vanilla container now, where they
        // must never collide with the game's own node labels.
        // "ENTRY" is the game's own default entry-node label: the vanilla Interacted and a picked
        // menu choice's Conversation both open a container at "ENTRY". Our nodes live in our own
        // container (never in the vanilla one), but the entry node must carry this exact label so
        // the injected choice opens our conversation instead of a blank one.
        private const string EntryNode = "ENTRY";
        private const string HowNode = "wvc_casino_how";
        private const string WhereNode = "wvc_casino_where";
        private const string CheckNode = "wvc_casino_check";
        private const string CollectNode = "wvc_casino_collect";
        private const string GiveOkNode = "wvc_casino_give_ok";
        private const string ClaimOkNode = "wvc_casino_claim_ok";

        private const string AskLaunder = "philip_launder_ask";
        private const string AskCollect = "philip_collect_ask";
        private const string AskHow = "philip_how";
        private const string AskWhere = "philip_where";
        private const string ConfirmLaunder = "philip_launder_confirm";
        private const string ConfirmClaim = "philip_claim_confirm";
        private const string LeaveChoice = "philip_leave";

        private static NPCDialogue _activeDialogue;
        private static NativeNpc _activeNative;
        private static NPCDialogue _hookedDialogue;
        private static bool _busy;

        /// <summary>True while a conversation is tracked against a live dialogue.</summary>
        public static bool IsActive => _activeDialogue != null;

        /// <summary>
        /// Builds the nodes, merges them into Philip's vanilla dialogue, hooks the two confirm
        /// callbacks (and a pre-open refresh) once per dialogue instance, and clears any leftover
        /// container override. Mirrors the register-and-arm pattern the other dialogues use.
        /// </summary>
        public static bool TryRegisterAndArm(NPCDialogue dialogue, NativeNpc native)
        {
            if (dialogue == null || native == null)
                return false;

            _activeDialogue = dialogue;
            _activeNative = native;

            try
            {
                // Build our own container and arm it as Philip's interact-dialogue through
                // S1API. The game's 0.4.7 dialogue rewrite replaced the old node-list container
                // with a new model, so the low-level clone/merge path no longer applies -
                // S1API's NPCDialogue abstraction is the supported way to do this now.
                //
                // Built ONCE per dialogue instance. Rebuilding the container while it is armed
                // (or while it is on screen) replaces the Conversation object the game is
                // holding, which on 0.4.7 leaves the interactable with no renderable dialogue -
                // the box never opens and the NPC stops being talkable until a restart. So we
                // register once and leave it stable; the dynamic wording is baked at build time.
                if (!BuildAndMerge(dialogue, native))
                    return false;

                if (!ReferenceEquals(_hookedDialogue, dialogue))
                {
                    dialogue.OnChoiceSelected(ConfirmLaunder, OnConfirmLaunder);
                    dialogue.OnChoiceSelected(ConfirmClaim, OnConfirmClaim);
                    _hookedDialogue = dialogue;
                }

                return true;
            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Always(
                    "[Casino] Register-and-arm failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Intentionally does NOT rebuild the armed container. On 0.4.7, replacing the
        /// Conversation object while it is armed (or on screen) orphans the object the game is
        /// holding, which breaks the interactable. The container is built once at arm time with
        /// the current wording; this is kept as a no-op so existing callers remain valid.
        /// </summary>
        public static void RebuildIfArmed()
        {
            // No-op by design - see above.
        }

        /// <summary>Hands the NPC his vanilla conversation back (kill switch).</summary>
        public static void StripFrom(NativeNpc native)
        {
            ClearOverrideContainer(native);
        }

        /// <summary>
        /// Rebuilds the container with LIVE state and injects our laundering option into Philip's
        /// own interact menu (alongside his vanilla options), so the E-press shows vanilla + ours
        /// together. Returns true if the option is present in the menu afterwards.
        /// </summary>
        public static bool InjectMenu(Il2CppScheduleOne.Dialogue.DialogueController controller)
        {
            var dialogue = _activeDialogue;
            if (dialogue == null || controller == null)
                return false;
            try
            {
                // Rebuild with live state (claim-ready / can-give) right before injecting.
                if (_activeNative != null)
                    BuildAndMerge(dialogue, _activeNative);

                bool ok = global::CustomNPCExample.Utils.WvcMenuInjector.AddOrRefresh(
                    controller, _activeNative, ContainerName, MenuChoiceText, null);
                return ok;
            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Always(
                    "[Casino] InjectMenu failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Directly opens our laundering conversation the instant the player interacts with Philip.
        /// Uses S1API's JumpTo (Handler.StartDialogue) rather than the interact override, because on
        /// a vanilla NPC in 0.4.7 the override container does not reliably drive the vanilla interact
        /// flow - the E-press shows the vanilla menu instead of our container. JumpTo forces our
        /// container to open regardless of what the vanilla interact would have shown.
        /// Returns true if a box was opened.
        /// </summary>
        public static bool OpenNow()
        {
            var dialogue = _activeDialogue;
            if (dialogue == null)
                return false;
            try
            {
                // Rebuild the container with LIVE state right before opening it. The conditional
                // choices (Collect when IsClaimReady, Hand-over when CanGive) and the baked
                // collect/check wording are evaluated here, so a rebuild at open time reflects
                // whether the two-day hold has elapsed and whether money is actually pending.
                // Safe because the interact prefix runs on a fresh E-press - no conversation is
                // on screen, so replacing the container cannot orphan a visible one.
                if (_activeNative != null)
                    BuildAndMerge(dialogue, _activeNative);

                bool opened = dialogue.JumpTo(ContainerName, EntryNode, true);
                return opened;
            }
            catch (Exception ex)
            {
                global::CustomNPCExample.Utils.WvcLog.Always(
                    "[Casino] OpenNow failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>Drops the cached dialogue (scene unload or kill switch).</summary>
        public static void Forget()
        {
            _activeDialogue = null;
            _activeNative = null;
        }

        private static bool BuildAndMerge(NPCDialogue dialogue, NativeNpc native)
        {
            // Build our nodes into our own container and arm it on interact. S1API registers
            // the container fresh each call, so rebuilding before every open keeps the wording
            // (cooldown, shortfall) current without any manual container surgery.
            dialogue.BuildAndRegisterContainer(ContainerName, builder =>
            {
                float cap = CasinoLaunderingManager.CurrentCap;
                int hold = CasinoLaunderingManager.CurrentHoldDays;
                float feePct = CasinoLaunderingManager.CurrentFeeFraction * 100f;
                float vipPct = CasinoLaunderingManager.CurrentVipBonusFraction * 100f;
                float payout = cap * (1f - CasinoLaunderingManager.CurrentFeeFraction) *
                               (1f + CasinoLaunderingManager.CurrentVipBonusFraction);

                string feeLine = feePct > 0.01f
                    ? "I take " + feePct.ToString("0.#") + "% as my cut, so $" +
                      payout.ToString("N0") + " lands clean for every $" + cap.ToString("N0") +
                      " you hand over."
                    : "No cut on a stack your size - the whole thing lands clean.";

                string vipLine = vipPct > 0.01f
                    ? " And you're Made now, so I add a " + vipPct.ToString("0.#") +
                      "% bonus on top for the trouble."
                    : "";

                builder.AddNode(
                    EntryNode,
                    "Philip Wentworth: The cage is open. Chips, cash, conversions - so long as the " +
                    "numbers behave, we're in business.\n\n" + CasinoLaunderingManager.StandingLine +
                    "\n\nWhat can I do for you?",
                    choices =>
                    {
                        choices.Add(AskLaunder, "I want to run $" + cap.ToString("N0") +
                            " through the cage.", CheckNode);
                        choices.Add(AskCollect, "I'm here to collect my money.", CollectNode);
                        choices.Add(AskHow, "How does it work?", HowNode);
                        choices.Add(AskWhere, "Where does the money go?", WhereNode);
                        choices.Add(LeaveChoice, "Just looking around.", null);
                    });

                builder.AddNode(
                    HowNode,
                    "Philip: You bring cash, I take it, and " + hold + " in-game day(s) later you " +
                    "come back and collect a clean amount in your account - it reads exactly like " +
                    "what the house paid out. No forms, no awkward questions.\n\n" +
                    feeLine + vipLine + "\n\n" +
                    "We only do it one stack at a time. A gap like that reads as a high-roller on " +
                    "a streak, not somebody scrubbing money.",
                    choices =>
                    {
                        choices.Add(AskLaunder, "Let's move it.", CheckNode);
                        choices.Add(AskCollect, "I'm here to collect my money.", CollectNode);
                        choices.Add(AskWhere, "Where does the money go?", WhereNode);
                        choices.Add(LeaveChoice, "Understood.", null);
                    });

                builder.AddNode(
                    WhereNode,
                    "Philip: It lands in your account looking exactly like what the house says you " +
                    "won. The casino's books balance, your books balance, and nobody has a reason " +
                    "to look closer.\n\nThat's the whole trick. Clean money is boring money.",
                    choices =>
                    {
                        choices.Add(AskHow, "How does it work?", HowNode);
                        choices.Add(AskCollect, "I'm here to collect my money.", CollectNode);
                        choices.Add(LeaveChoice, "Good enough for me.", null);
                    });

                builder.AddNode(
                    CheckNode,
                    BuildCheckText(),
                    choices =>
                    {
                        if (CasinoLaunderingManager.CanGive(
                                CasinoLaunderingManager.CurrentCap, false, out _))
                        {
                            choices.Add(ConfirmLaunder, "Hand over the $" +
                                CasinoLaunderingManager.CurrentCap.ToString("N0") + ".",
                                GiveOkNode);
                        }

                        choices.Add(LeaveChoice, "Maybe later.", null);
                    });

                builder.AddNode(
                    CollectNode,
                    BuildCollectText(),
                    choices =>
                    {
                        if (CasinoLaunderingManager.IsClaimReady)
                        {
                            choices.Add(ConfirmClaim, "Collect my money.", ClaimOkNode);
                        }

                        choices.Add(LeaveChoice, "Not yet.", null);
                    });

                builder.AddNode(
                    GiveOkNode,
                    "Philip: Pleasure. The cash's with me - the books sit on it for " + hold +
                    " in-game day(s). Come back after that and collect it clean.\n\n" +
                    "When you do, $" + payout.ToString("N0") + " lands in your account. Until " +
                    "then, don't spend it twice.",
                    choices =>
                    {
                        choices.Add(LeaveChoice, "Thanks, Philip.", null);
                    });

                builder.AddNode(
                    ClaimOkNode,
                    "Philip: There it is - clean, straight to your account, and it reads exactly " +
                    "like a normal night's winnings.\n\n" + CasinoLaunderingManager.StandingLine +
                    "\n\nWhenever you want to run another stack through, you know where the cage " +
                    "is.",
                    choices =>
                    {
                        choices.Add(LeaveChoice, "Thanks, Philip.", null);
                    });

                // (every node is built here - entry, flavour, check, collect and the two
                // success nodes)
            });

            // (the container is already registered by the BuildAndRegisterContainer call above.
            // Deliberately NOT armed as Philip's interact-dialogue - no UseContainerOnInteract:
            // on 0.4.7 that sets the native OverrideContainer, which makes the vanilla Interacted
            // open OUR container at its default "ENTRY" node instead of his own menu - a blank,
            // un-talkable box. We only need it registered so our injected menu choice can
            // reference it; vanilla stays the interact target and shows his options plus ours.)
            return true;
        }

        private static void ClearOverrideContainer(NativeNpc native)
        {
            // Kill switch: hand Philip his vanilla conversation back. S1API clears the
            // override on the wrapped dialogue; the native controller call is a best-effort
            // fallback for when the wrapper is not cached yet.
            try
            {
                _activeDialogue?.StopOverride();
            }
            catch (Exception)
            {
            }

            try
            {
                var controllers = native.GetComponentsInChildren<
                    Il2CppScheduleOne.Dialogue.DialogueController>(true);

                if (controllers == null)
                    return;

                foreach (var controller in controllers)
                {
                    if (controller != null)
                        controller.ClearOverrideContainer();
                }
            }
            catch (Exception)
            {
            }
        }

        private static string BuildCheckText()
        {
            if (CasinoLaunderingManager.HasPending)
            {
                return
                    "Philip: Not so fast - you already have $" +
                    CasinoLaunderingManager.PendingAmount.ToString("N0") +
                    " waiting on the books. Collect that first, then we'll talk about the next " +
                    "stack.";
            }

            float cap = CasinoLaunderingManager.CurrentCap;
            int hold = CasinoLaunderingManager.CurrentHoldDays;
            float payout = cap * (1f - CasinoLaunderingManager.CurrentFeeFraction) *
                           (1f + CasinoLaunderingManager.CurrentVipBonusFraction);

            float missing = CasinoLaunderingManager.CashShortfall(cap);

            if (missing > 0f)
            {
                return
                    "Philip: You're $" + missing.ToString("N0") + " short of " +
                    cap.ToString("N0") + ". The cage moves full stacks only - partial numbers " +
                    "are how people get noticed.\n\nCome back with the whole amount.";
            }

            return
                "Philip: $" + cap.ToString("N0") + " in cash, on the books for " + hold +
                " in-game day(s), then $" + payout.ToString("N0") +
                " clean in your account. We're clear to move it right now.";
        }

        private static string BuildCollectText()
        {
            if (!CasinoLaunderingManager.HasPending)
            {
                return
                    "Philip: Nothing of yours on the books right now. Bring a stack and we'll " +
                    "start the clock.";
            }

            if (!CasinoLaunderingManager.IsClaimReady)
            {
                int hold = CasinoLaunderingManager.PendingHoldDays;
                int elapsed = hold - CasinoLaunderingManager.ClaimDaysRemaining;
                return
                    "Philip: Not yet. The books need " + hold +
                    " full in-game day(s) before it reads clean - yours has been up for " +
                    elapsed + ", with " + CasinoLaunderingManager.ClaimDaysRemaining +
                    " day(s) to go. Come back then.";
            }

            return
                "Philip: $" + CasinoLaunderingManager.PendingAmount.ToString("N0") +
                " of yours, clean and ready - $" +
                CasinoLaunderingManager.PendingPayout.ToString("N0") +
                " lands in your account. Say the word.";
        }

        private static void OnConfirmLaunder()
        {
            if (_busy)
                return;

            _busy = true;

            try
            {
                bool ok = CasinoLaunderingManager.TryGive(
                    CasinoLaunderingManager.CurrentCap, false, out string reason);

                if (ok)
                {
                    // RebuildIfArmed already ran inside TryGive, so the next opening shows the
                    // waiting-money wording.
                    return;
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Handover refused at the cage: " + reason);

                // The success line must not stand next to balances that never moved.
                try
                {
                    _activeDialogue?.End();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error("[Casino] Give callback exception: " + ex);

                try
                {
                    _activeDialogue?.End();
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private static void OnConfirmClaim()
        {
            if (_busy)
                return;

            _busy = true;

            try
            {
                bool ok = CasinoLaunderingManager.TryClaim(false, out string reason);

                if (ok)
                    return;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Casino] Claim refused at the cage: " + reason);

                try
                {
                    _activeDialogue?.End();
                }
                catch (Exception)
                {
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error("[Casino] Claim callback exception: " + ex);

                try
                {
                    _activeDialogue?.End();
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
