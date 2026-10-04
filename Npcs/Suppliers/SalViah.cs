using CustomNPCExample.Products;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Map;
using System;
using System.Collections;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class SalViah : NPC
    {
        public static SalViah Instance { get; private set; }

        public const string NpcId = "custom_sal_viah";
        public const string SupplierPersistentId = "wvc_sal_viah_v1";

        private const string PaymentDropName = "Sal's Gazebo Stash";
        private const string PaymentDropDescription =
            "Sal Viah's drop spot at the park Gazebo. Leave what you owe by the wooden pillars. " +
            "The spirits and runners know when the cash lands.";

        private static readonly Vector3 DefaultSpawnPosition =
            new Vector3(-18.2f, -0.76f, 36.5f);

        private static readonly Quaternion DefaultSpawnRotation =
            Quaternion.Euler(0f, 135f, 0f);

        private bool _dialogueSetupStarted;
        private bool _deadDropSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] ConfigurePrefab started.");

            Building northApartments = NPCBuildingLookup.Get("North apartments");
            Building cafe            = NPCBuildingLookup.Get("Cafe");
            Building hardware        = NPCBuildingLookup.Get("Dan's Hardware Upstairs");
            Building supermarket     = NPCBuildingLookup.Get("Supermarket");
            Building budsBar         = NPCBuildingLookup.Get("Bud's Bar");
            Building nightclub       = NPCBuildingLookup.Get("Nightclub");
            Building gasMart         = NPCBuildingLookup.Get("West Gas-Mart");
            Building pawnShop        = NPCBuildingLookup.Get("Pawn Shop");
            Building motel           = NPCBuildingLookup.Get("Motel Office");

            Building fallback = cafe ?? budsBar ?? northApartments ?? motel ?? gasMart ?? pawnShop;

            builder
                .WithIdentity(
                    NpcId,
                    "Sal",
                    "Viah"
                )
                .WithRegion(
                    Region.Westville
                )
                .WithSpawnPosition(
                    DefaultSpawnPosition,
                    DefaultSpawnRotation
                )
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.98f;
                    av.Weight = 0.54f;

                    av.SkinColor =
                        new Color32(
                            208,
                            168,
                            138,
                            255
                        );

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint =
                        new Color(
                            0.84f,
                            0.88f,
                            0.80f
                        );

                    av.PupilDilation = 0.82f;

                    av.EyebrowScale = 1.08f;
                    av.EyebrowThickness = 1.12f;
                    av.EyebrowRestingHeight = 0.02f;
                    av.EyebrowRestingAngle = -4f;

                    av.LeftEye = new ValueTuple<float, float>(0.42f, 0.48f);
                    av.RightEye = new ValueTuple<float, float>(0.42f, 0.48f);

                    av.HairPath =
                        "Avatar/Hair/LongCurly/LongCurly";

                    av.HairColor =
                        new Color32(
                            52,
                            38,
                            28,
                            255
                        );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SlightSmile",
                        new Color(
                            0.12f,
                            0.10f,
                            0.08f
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Goatee",
                        new Color32(
                            52,
                            38,
                            28,
                            255
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/TiredEyes",
                        new Color(
                            0.42f,
                            0.25f,
                            0.22f,
                            0.60f
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Freckles",
                        new Color32(
                            118,
                            78,
                            48,
                            160
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/RolledButtonup",
                        new Color32(
                            82,
                            114,
                            88,
                            255
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/CargoPants",
                        new Color32(
                            68,
                            65,
                            56,
                            255
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Tattoos/LeftArm/LeftArm_Alien",
                        new Color32(
                            35,
                            85,
                            55,
                            220
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/OpenVest/OpenVest",
                        new Color32(
                            115,
                            82,
                            54,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/SmallRoundGlasses/SmallRoundGlasses",
                        new Color32(
                            195,
                            155,
                            65,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sandals/Sandals",
                        new Color32(
                            58,
                            42,
                            30,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Neck/GoldChain/GoldChain",
                        new Color32(
                            215,
                            175,
                            55,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color32(
                            48,
                            34,
                            22,
                            255
                        )
                    );
                })
                .WithSupplierDefaults(s => s
                    .WithPersistentId(
                        SupplierPersistentId
                    )
                    .WithOrderLimits(
                        50f,
                        5000f
                    )
                    .WithStashDeadDrop<Gazebo>()

                    .WithDeliveryItem(
                        SalviaSeed.SeedId
                    )

                    .WithRecommendationMessage(
                        "Peace and light, friend... If your mind seeks horizons beyond the mundane, " +
                        "I know a keeper who cultivates sacred Salvia Divinorum cuttings. " +
                        "I passed your number to Sal Viah, expect a transmission soon."
                    )
                    .WithUnlockHint(
                        "Sal Viah is now available. You can order Salvia cuttings from Sal. " +
                        "Payments go to the park Gazebo."
                    )
                )
                .WithRelationshipDefaults(r => r
                    .WithDelta(
                        2.0f
                    )
                    .SetUnlocked(
                        false
                    )
                    .SetUnlockType(
                        NPCRelationship.UnlockType.Recommendation
                    )
                    .WithConnectionsById(
                        new[]
                        {
                            // Jeff Gilmore is the one who vouches for Sal's cuttings.
                            "jeff_gilmore"
                        }
                    )
                )
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {

                        return;
                    }

                    // Trade hours start the moment the player wakes up, so a morning run never ends
                    // at empty hands.
                    plan.StayInBuilding(cafe ?? fallback, 0, 1395, null, "Trade Hours");

                    plan.StayInBuilding(northApartments ?? fallback, 1395, 45, null, "Rest");

                    global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] Trade-hours schedule assigned.");
                });

            global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.02f;
                Region = Region.Downtown;

                Appearance.Build();
                Schedule.Enable();

                Relationship.OnUnlocked -= HandleUnlocked;
                Relationship.OnUnlocked += HandleUnlocked;

                if (Relationship.IsUnlocked)
                {
                    UnlockSupplierSilently();
                }

                if (!_dialogueSetupStarted)
                {
                    _dialogueSetupStarted = true;
                    MelonCoroutines.Start(
                        ConfigureDialogueAfterLoad()
                    );
                }

                if (!_deadDropSetupStarted)
                {
                    _deadDropSetupStarted = true;
                    MelonCoroutines.Start(
                        ConfigureExistingDeadDropAfterLoad()
                    );
                }

                global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] Sal Viah loaded as Salvia supplier.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[SalViah] OnCreated failed: " + ex);
            }
        }

        protected override void OnDestroyed()
        {
            Relationship.OnUnlocked -= HandleUnlocked;

            if (ReferenceEquals(Instance, this))
                Instance = null;

            base.OnDestroyed();
        }

        private void HandleUnlocked(
            NPCRelationship.UnlockType _,
            bool notify)
        {
            if (notify)
            {
                SendTextMessage(
                    "Greetings... Sal here. The wind tells me you walk a path of curiosity. " +
                    "I have tuned my frequency to your supplier list. " +
                    "Check your phone when you are ready to explore the sacred foliage."
                );
            }

            UnlockSupplierSilently();
        }

        private void UnlockSupplierSilently()
        {
            try
            {
                if (Supplier != null && Supplier.IsSupplier)
                {
                    Supplier.Unlock();
                    global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] Supplier unlocked and products visible.");
                }
            }
            catch (Exception)
            {

            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int i = 0; i < 80; i++)
            {
                try
                {
                    if (Dialogue != null &&
                        SalViahDialogue.TryRegisterAndArm(Dialogue))
                    {
                        global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] Salvia dialogue armed.");
                        yield break;
                    }
                }
                catch (Exception)
                {

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
                    DeadDropManager.Get<Gazebo>();

                if (selectedDrop == null)
                    return false;

                string guid = selectedDrop.GUID;
                if (string.IsNullOrWhiteSpace(guid))
                    return false;

                var nativeDrops = DeadDrop.DeadDrops;
                if (nativeDrops == null || nativeDrops.Count == 0)
                    return false;

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop = nativeDrops[i];
                    if (drop == null)
                        continue;

                    bool match =
                        string.Equals(drop.GUID.ToString(), guid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(drop.BakedGUID, guid, StringComparison.OrdinalIgnoreCase);

                    if (!match)
                        continue;

                    drop.DeadDropName = PaymentDropName;
                    drop.DeadDropDescription = PaymentDropDescription;

                    global::CustomNPCExample.Utils.WvcLog.Msg("[SalViah] Dead drop claimed: " + guid);
                    return true;
                }
            }
            catch (Exception)
            {

            }

            return false;
        }
    }
}
