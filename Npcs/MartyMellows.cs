using CustomNPCExample.Products.Edibles;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using S1API.DeadDrops;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using S1API.Map;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class MartyMellows : NPC
    {
        private static readonly Vector3 HiddenSpawnPosition =
            new Vector3(-50f, 1.06f, 70f);

        private static MartyMellows _activeInstance;
        private static bool _isAvailable;
        private static bool _avatarFixApplied;

        public static MartyMellows Instance => _activeInstance;

        public const string NpcId = "custom_marty_mellows";
        public const string SupplierPersistentId = "wvc_marty_mellows";

        private const string PaymentDropName = "Marty's Payment Drop";
        private const string PaymentDropDescription =
            "Marty's payment drop behind the Slop Shop. " +
            "Leave what you owe here. He collects after close.";

        private bool _deadDropSetupStarted;
        private bool _dialogueSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        internal static bool IsAvailable => _isAvailable;
        internal static event Action<bool> AvailabilityChanged;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            EnsureAvatarBypass();

            MelonLogger.Msg("[Marty] ConfigurePrefab started.");

            Building slopShop = NPCBuildingLookup.Get("Slop Shop");

            builder
                .WithIdentity(NpcId, "Marty", "Mellows")
                .WithVoice(NPCVoiceCatalog.Tyler, 1.02f)
                .WithRegion(Region.Suburbia)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.98f;
                    av.Weight = 0.52f;

                    av.SkinColor = new Color(0.66f, 0.50f, 0.40f);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.92f, 0.95f, 0.96f);
                    av.PupilDilation = 0.52f;

                    av.EyebrowScale = 1.08f;
                    av.EyebrowThickness = 1.05f;
                    av.EyebrowRestingHeight = -0.12f;
                    av.EyebrowRestingAngle = -3f;

                    av.LeftEye = new ValueTuple<float, float>(0.40f, 0.48f);
                    av.RightEye = new ValueTuple<float, float>(0.40f, 0.48f);

                    av.HairPath = "Avatar/Hair/Spiky/Spiky";
                    av.HairColor = new Color(0.12f, 0.08f, 0.06f);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        new Color(0.10f, 0.10f, 0.10f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color(0.24f, 0.72f, 0.58f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(0.14f, 0.14f, 0.18f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                        new Color(0.88f, 0.36f, 0.22f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color(0.92f, 0.92f, 0.92f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color(0.22f, 0.15f, 0.10f)
                    );

                    av.WithImpostor("Benji");
                })
                .WithSpawnPosition(HiddenSpawnPosition)
                .EnsureSupplier()
                .WithSupplierDefaults(s => s
                    .WithPersistentId(SupplierPersistentId)
                    .WithOrderLimits(100f, 2000f)
                    .WithDeliveryItem(GummyIngredients.ThcOilId)
                    .WithDeliveryItem(GummyIngredients.GelatinId)
                    .WithStashDeadDrop<S1API.DeadDrops.Native.BehindSlopShop>()
                    .WithRecommendationMessage(
                        "My friend Marty runs the kitchen side. " +
                        "He can get you THC oil and " +
                        "Gelatin. I gave him your number."
                    )
                    .WithUnlockHint(
                        "You can now order THC Oil and " +
                        "Gelatin from Marty at the " +
                        "Slop Shop. Sugar can be bought at the Gas-Mart."
                    ))
                .WithRelationshipDefaults(r => r
                    .WithDelta(2.0f)
                    .SetUnlocked(false)   // CHANGED: Starts locked
                    .SetUnlockType(NPCRelationship.UnlockType.Recommendation)
                    .WithConnectionsById(new[]
                    {
                        "hank_stevenson"
                    }))
                .WithSchedule(plan =>
                {
                    if (slopShop != null)
                    {
                        plan.StayInBuilding(slopShop, 0, 1440, null, null);
                        MelonLogger.Msg("[Marty] Schedule assigned: Slop Shop, 00:00-24:00.");
                    }
                    else
                    {
                        MelonLogger.Warning("[Marty] Slop Shop was not found.");
                    }
                });

            MelonLogger.Msg("[Marty] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                _activeInstance = this;
                Region = Region.Suburbia;

                Appearance.Build();
                Schedule.Enable();

                RefreshAvailabilitySubscription();

                MelonLogger.Msg("[Marty] Marty Mellows loaded as a supplier at Slop Shop.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Marty] OnCreated failed: " + ex);
            }

            if (!_dialogueSetupStarted)
            {
                _dialogueSetupStarted = true;
                MelonCoroutines.Start(ConfigureDialogueAfterLoad());
            }

            if (!_deadDropSetupStarted)
            {
                _deadDropSetupStarted = true;
                MelonCoroutines.Start(ConfigureExistingDeadDropAfterLoad());
            }
        }

        protected override void OnDestroyed()
        {
            Relationship.OnUnlocked -= HandleUnlocked;

            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;

            PublishAvailability(false);
            base.OnDestroyed();
        }

        private void HandleUnlocked(
            NPCRelationship.UnlockType _,
            bool notify)
        {
            PublishAvailability(true);

            if (notify)
            {
                SendTextMessage(
                    "Marty here. Hank said you're good people. " +
                    "I can get you THC oil and gelatin. " +
                    "Check your phone, I'm in your suppliers now."
                );
            }

            // Activate the supplier so products show up
            try
            {
                if (Supplier != null && Supplier.IsSupplier)
                {
                    Supplier.Unlock();

                    MelonLogger.Msg(
                        "[Marty] Supplier unlocked and products visible."
                    );
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Marty] Supplier unlock failed: " +
                    ex.Message
                );
            }
        }

        private void RefreshAvailabilitySubscription()
        {
            Relationship.OnUnlocked -= HandleUnlocked;
            Relationship.OnUnlocked += HandleUnlocked;
            PublishAvailability(Relationship.IsUnlocked);
        }

        private static void PublishAvailability(bool available)
        {
            if (_isAvailable == available) return;
            _isAvailable = available;
            AvailabilityChanged?.Invoke(available);

            MelonLogger.Msg($"[Marty] Availability changed: {available}");
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int i = 0; i < 80; i++)
            {
                try
                {
                    if (Dialogue != null &&
                        MartyMellowsDialogue.TryRegisterAndArm(Dialogue))
                    {
                        MelonLogger.Msg("[Marty] Gummies dialogue armed.");
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning("[Marty] Dialogue failed: " + ex.Message);
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator ConfigureExistingDeadDropAfterLoad()
        {
            for (int i = 0; i < 80; i++)
            {
                if (TryConfigureExistingDeadDrop())
                    yield break;
                yield return new WaitForSeconds(0.25f);
            }
        }

        private static bool TryConfigureExistingDeadDrop()
        {
            try
            {
                DeadDropInstance selectedDrop =
                    DeadDropManager.Get<S1API.DeadDrops.Native.BehindSlopShop>();

                if (selectedDrop == null) return false;

                string guid = selectedDrop.GUID;
                if (string.IsNullOrWhiteSpace(guid)) return false;

                var nativeDrops = DeadDrop.DeadDrops;
                if (nativeDrops == null || nativeDrops.Count == 0) return false;

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop = nativeDrops[i];
                    if (drop == null) continue;

                    bool match =
                        string.Equals(drop.GUID.ToString(), guid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(drop.BakedGUID, guid, StringComparison.OrdinalIgnoreCase);

                    if (!match) continue;

                    drop.DeadDropName = PaymentDropName;
                    drop.DeadDropDescription = PaymentDropDescription;

                    MelonLogger.Msg("[Marty] Dead drop claimed: " + guid);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[Marty] Dead drop setup failed: " + ex.Message);
            }

            return false;
        }

        private static void EnsureAvatarBypass()
        {
            if (_avatarFixApplied) return;
            _avatarFixApplied = true;

            try
            {
                HarmonyLib.Harmony harmony =
                    new HarmonyLib.Harmony("wvc.marty.avatarfix");

                MethodInfo method =
                    HarmonyLib.AccessTools.Method(
                        typeof(NPC),
                        "TryValidateNativeAwakeReferences"
                    );

                if (method == null)
                {
                    MelonLogger.Warning("[MartyFix] Validator method not found.");
                    return;
                }

                HarmonyLib.HarmonyMethod prefix =
                    new HarmonyLib.HarmonyMethod(
                        typeof(MartyMellows).GetMethod(
                            nameof(AvatarBypass),
                            BindingFlags.NonPublic | BindingFlags.Static
                        )
                    );

                harmony.Patch(method, prefix: prefix);

                MelonLogger.Msg("[MartyFix] Avatar validation bypass installed.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[MartyFix] Failed: " + ex);
            }
        }

        private static bool AvatarBypass(
            NPC __instance,
            out string diagnostic,
            ref bool __result)
        {
            if (__instance is MartyMellows)
            {
                diagnostic = string.Empty;
                __result = true;
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}