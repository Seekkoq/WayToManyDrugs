using System;
using MelonLoader;
using S1API.Entities;

namespace CustomNPCExample.NPCs
{
    public static class OffshoreDialogue
    {
        public const string ContainerIdle = "wvc_declan_idle";
        public const string ContainerAwaiting = "wvc_declan_awaiting";
        public const string ContainerPreparing = "wvc_declan_preparing";
        public const string ContainerPayout = "wvc_declan_payout";

        private const string EntryNode = "ENTRY";
        private const string LoreNode = "LORE";
        private const string HandoverCheckNode = "HANDOVER_CHECK";
        private const string PayoutCheckNode = "PAYOUT_CHECK";
        private const string WalkthroughNode = "WALKTHROUGH";
        private const string HeatLoreNode = "HEAT_LORE";

        private const string HandoverChoiceId = "declan_handover_init";
        private const string HandoverConfirmChoiceId = "declan_handover_confirm";
        private const string PayoutChoiceId = "declan_payout_init";
        private const string PayoutConfirmChoiceId = "declan_payout_confirm";
        private const string ChatChoiceId = "declan_chat_lore";
        private const string LeaveChoiceId = "declan_leave";
        private const string WalkthroughChoiceId = "declan_walkthrough";
        private const string WalkthroughStartChoiceId = "declan_walkthrough_start";
        private const string HeatChoiceId = "declan_heat_lore";

        private static NPCDialogue _activeDialogue;
        private static bool _callbacksHooked;
        private static bool _containersRegistered;

        public static bool TryRegisterAndArm(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return false;

            _activeDialogue = dialogue;

            try
            {
                if (!_containersRegistered)
                {
                    dialogue.BuildAndRegisterContainer(ContainerIdle, builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(EntryNode,
                            "Declan: Ahoy. Name's Declan Cross. I run offshore cargo for foreign buyers across the waters. Keep your phone on and watch for my texts. When an overseas client orders a run, I'll text you.",
                            choices =>
                            {
                                choices.Add(WalkthroughChoiceId, "Walk me through an offshore deal.", WalkthroughNode);
                                choices.Add(ChatChoiceId, "Tell me about your offshore operations.", LoreNode);
                                choices.Add(HeatChoiceId, "How much heat are we in right now?", HeatLoreNode);
                                choices.Add(LeaveChoiceId, "I'll see you around, Declan.", null);
                            });

                        builder.AddNode(LoreNode,
                            "Declan: I smuggle high-grade freight across international shipping lanes. Out there, syndicates pay serious cash for clean product: MDMA, Cocaine, DMT, Salvia, and THC Gummies. " +
                            "When an order opens up, I'll text you. You bring the cargo to the docks, I make the crossing, and we split the take.",
                            choices =>
                            {
                                choices.Add(LeaveChoiceId, "Understood. Safe sailing.", null);
                            });

                        builder.AddNode(HeatLoreNode,
                            "Declan: Every run we sail, more people take notice - coast guard, customs, rival syndicates. They all watch the Night Wave sooner or later. " +
                            "The hotter the lane gets, the likelier a shipment gets seized, and I space the orders out to match. " +
                            "Push too hard and the ports boil over - then nobody sails until things cool off, no matter how much product you're sitting on. " +
                            "Lay low a few in-game days and the heat always bleeds away. " +
                            "Text me 'Shipment status?' any time and I'll tell you exactly how hot the water is. " +
                            "And mind this: hot waters pay a danger premium. Risk and reward sail on the same boat.",
                            choices =>
                            {
                                choices.Add(LeaveChoiceId, "Good to know. I'll pace the runs.", null);
                            });

                        builder.AddNode(WalkthroughNode,
                            "Declan: Aye, listen close, because this is the whole run, start to finish. " +
                            "One: keep your phone on. When an overseas buyer opens an order, I'll text you the manifest - the product, the quantity, and the destination port. " +
                            "Two: cook or source exactly what the manifest asks for, and bring it down here to the pier. " +
                            "Three: talk to me and hand the cargo over - I check it against the manifest before it goes in the hold. " +
                            "Four: I make the crossing myself. The voyage takes a while, so keep yourself busy in town. " +
                            "Five: when I text you that we've made port, come back to the docks and I'll pay out your cut in cash, on the barrel. " +
                            "And mind the heat - every run we sail draws eyes to the lane. Push too hard and the ports boil over, and then nobody sails until things cool off. " +
                            "So - are you ready to sail?",
                            choices =>
                            {
                                choices.Add(WalkthroughStartChoiceId, "Aye, let's line up a run right now.", null);
                                choices.Add(LeaveChoiceId, "Not yet. I'll wait for your text.", null);
                            });
                    });

                    dialogue.BuildAndRegisterContainer(ContainerAwaiting, builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(EntryNode,
                            "Declan: The buyers overseas are waiting on a shipment. Give me a minute to get down to the boat, then bring the cargo to the pier.",
                            choices =>
                            {
                                choices.Add(HandoverChoiceId, "I have the overseas shipment with me.", HandoverCheckNode);
                                choices.Add(ChatChoiceId, "Tell me about your offshore operations.", LoreNode);
                                choices.Add(HeatChoiceId, "How much heat are we in right now?", HeatLoreNode);
                                choices.Add(LeaveChoiceId, "I'll be right back with the cargo.", null);
                            });

                        builder.AddNode(HeatLoreNode,
                            "Declan: Same as ever - every run leaves a wake. Coast guard patrols, customs sweeps, syndicate informants. " +
                            "If the lane's hot, this shipment sails with more risk than the last one. Text me for the live status and I'll read you the water.",
                            choices =>
                            {
                                choices.Add(LeaveChoiceId, "Understood.", null);
                            });

                        builder.AddNode(HandoverCheckNode,
                            "Declan: Let me inspect the cargo and check the packing against the manifest.",
                            choices =>
                            {
                                choices.Add(HandoverConfirmChoiceId, "Hand over the shipment.", null);
                                choices.Add(LeaveChoiceId, "Wait, let me double check my bags.", null);
                            });

                        builder.AddNode(LoreNode,
                            "Declan: Cargo out here has to be pure and properly sealed for sea voyages. High humidity and salt water will ruin sloppy product.",
                            choices =>
                            {
                                choices.Add(HeatChoiceId, "How much heat are we in right now?", HeatLoreNode);
                                choices.Add(LeaveChoiceId, "Understood.", null);
                            });
                    });

                    dialogue.BuildAndRegisterContainer(ContainerPreparing, builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(EntryNode,
                            "Declan: We're lashing down the crates and checking the engine. Casting off in a few moments. Keep your phone on. I'll text you when I make port with the cash.",
                            choices =>
                            {
                                choices.Add(LeaveChoiceId, "Fair winds, Captain.", null);
                            });
                    });

                    dialogue.BuildAndRegisterContainer(ContainerPayout, builder =>
                    {
                        builder.SetAllowExit(true);

                        builder.AddNode(EntryNode,
                            "Declan: Welcome back! The overseas voyage went off without a hitch. The foreign buyers took the lot and paid top dollar. Ready to collect your cut?",
                            choices =>
                            {
                                choices.Add(PayoutChoiceId, "I'm here to collect my offshore payout.", PayoutCheckNode);
                                choices.Add(ChatChoiceId, "Tell me about your offshore operations.", LoreNode);
                                choices.Add(LeaveChoiceId, "I'll collect it later, Declan.", null);
                            });

                        builder.AddNode(PayoutCheckNode,
                            "Declan: Cash on the barrelhead. Let's count out the bills and settle the manifest.",
                            choices =>
                            {
                                choices.Add(PayoutConfirmChoiceId, "Collect payment.", null);
                                choices.Add(LeaveChoiceId, "I'll take it later.", null);
                            });

                        builder.AddNode(LoreNode,
                            "Declan: Fast ships and clean contacts make for smooth payouts. Stick with me and we'll both get rich.",
                            choices =>
                            {
                                choices.Add(LeaveChoiceId, "Pleasure doing business.", null);
                            });
                    });

                    _containersRegistered = true;
                }

                if (!_callbacksHooked)
                {
                    dialogue.OnChoiceSelected(HandoverConfirmChoiceId, OnHandoverSelected);
                    dialogue.OnChoiceSelected(PayoutConfirmChoiceId, OnPayoutSelected);
                    dialogue.OnChoiceSelected(WalkthroughStartChoiceId, OnStartDealSelected);
                    _callbacksHooked = true;
                }

                SyncActiveContainer(dialogue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void SyncActiveContainer(NPCDialogue dialogue)
        {
            if (dialogue == null)
                return;

            try
            {
                switch (OffshoreSalesManager.State)
                {
                    case OffshoreState.AwaitingProduct:
                        dialogue.UseContainerOnInteract(ContainerAwaiting);
                        break;

                    case OffshoreState.PreparingDeparture:
                        dialogue.UseContainerOnInteract(ContainerPreparing);
                        break;

                    case OffshoreState.ReturnedWithCash:
                        dialogue.UseContainerOnInteract(ContainerPayout);
                        break;

                    default:
                        dialogue.UseContainerOnInteract(ContainerIdle);
                        break;
                }
            }
            catch (Exception)
            {

            }
        }

        public static bool Register(NPCDialogue dialogue)
        {
            return TryRegisterAndArm(dialogue);
        }

        private static void OnStartDealSelected()
        {
            var state = OffshoreSalesManager.State;
            if (state != OffshoreState.Idle)
            {
                OffshoreSalesManager.SendDeclanMessage(
                    "Declan: Easy, sailor. We've already got a run on the books. See that one through before we line up another.");
                return;
            }

            bool started = OffshoreSalesManager.TriggerOffshoreSale(true);
            if (started)
            {
                SyncActiveContainer(_activeDialogue);
            }
            else
            {
                OffshoreSalesManager.SendDeclanMessage(
                    "Declan: Give me some time to line up a buyer. I'll text you when the manifest is ready.");
            }
        }

        private static void OnHandoverSelected()
        {
            var state = OffshoreSalesManager.State;
            var order = OffshoreSalesManager.CurrentOrder;

            if (state != OffshoreState.AwaitingProduct)
            {
                OffshoreSalesManager.SendDeclanMessage("Declan: We don't have an active offshore shipment order right now.");
                return;
            }

            int count = OffshoreSalesManager.CountPlayerDrug(order);
            if (count < order.RequiredQuantity)
            {
                string otherDrugs = OffshoreSalesManager.GetPlayerOtherHighProfitDrugs(order);
                if (!string.IsNullOrEmpty(otherDrugs))
                {
                    OffshoreSalesManager.SendDeclanMessage(
                        $"Declan: I can't take that. The buyers in {order.Destination} specifically ordered {order.RequiredQuantity}x {order.DrugDisplayName}, not {otherDrugs}. Bring me the {order.DrugDisplayName}."
                    );
                }
                else if (count == 0)
                {
                    OffshoreSalesManager.SendDeclanMessage(
                        $"Declan: The manifest calls for {order.RequiredQuantity}x {order.DrugDisplayName}, but I count {count}x on you. I need the full load before we can set sail."
                    );
                }
                else
                {
                    OffshoreSalesManager.SendDeclanMessage(
                        $"Declan: You only brought {count}/{order.RequiredQuantity}x {order.DrugDisplayName}. The manifest requires the full {order.RequiredQuantity} units. Come back when you have the entire shipment."
                    );
                }
                return;
            }

            bool submitted = OffshoreSalesManager.PlayerSubmitProduct();
            if (submitted)
            {
                SyncActiveContainer(_activeDialogue);
            }
        }

        private static void OnPayoutSelected()
        {
            var state = OffshoreSalesManager.State;
            if (state != OffshoreState.ReturnedWithCash)
            {
                if (state == OffshoreState.VoyageActive || state == OffshoreState.PreparingDeparture)
                {
                    OffshoreSalesManager.SendDeclanMessage("Declan: I haven't completed the overseas run yet. Hold tight.");
                }
                else
                {
                    OffshoreSalesManager.SendDeclanMessage("Declan: No outstanding offshore payouts on the books right now.");
                }
                return;
            }

            bool collected = OffshoreSalesManager.PlayerCollectPayout();
            if (collected)
            {
                SyncActiveContainer(_activeDialogue);
            }
        }
    }
}
