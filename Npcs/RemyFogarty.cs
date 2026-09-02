using CustomNPCExample.Products;
using CustomNPCExample.Products.Edibles;
using CustomNPCExample.Quests;
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
    public sealed class RemyFogarty : NPC
    {
        public static RemyFogarty Instance { get; private set; }

        public const string NpcId = "custom_remy_fogarty";
        public const string SupplierPersistentId = "wvc_remy_fogarty_v1";

        private const string PaymentDropName = "Fog's Fountain Drop";
        private const string PaymentDropDescription =
            "Remy's payment spot at the Town Hall fountain. Drop the cash in the water side stash. He'll know.";

        private bool _dialogueSetupStarted;
        private bool _deadDropSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Remy] ConfigurePrefab started.");

            Building motel = NPCBuildingLookup.Get("Motel Office");
            Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
            Building pawnShop = NPCBuildingLookup.Get("Pawn Shop");
            Building cafe = NPCBuildingLookup.Get("Cafe");
            Building arcade = NPCBuildingLookup.Get("Arcade");
            Building budsBar = NPCBuildingLookup.Get("Bud's Bar");
            Building nightclub = NPCBuildingLookup.Get("Nightclub");
            Building apartments = NPCBuildingLookup.Get("North apartments");

            Building fallback = apartments ?? motel ?? gasMart ?? cafe ?? arcade ?? budsBar ?? nightclub ?? pawnShop;
            Vector3 spawnPosition = new Vector3(-12.4f, -0.76f, 42.8f);

            builder
                .WithIdentity(NpcId, "Remy", "Fogarty")
                .WithRegion(Region.Downtown)
                .WithSpawnPosition(spawnPosition, Quaternion.Euler(0f, 210f, 0f))
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.97f;
                    av.Weight = 0.38f;

                    av.SkinColor = new Color32(210, 178, 158, 255);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(0.90f, 0.88f, 0.86f);
                    av.PupilDilation = 0.92f;

                    av.EyebrowScale = 0.85f;
                    av.EyebrowThickness = 0.70f;
                    av.EyebrowRestingHeight = 0.12f;
                    av.EyebrowRestingAngle = -11f;

                    av.LeftEye = new ValueTuple<float, float>(0.42f, 0.48f);
                    av.RightEye = new ValueTuple<float, float>(0.42f, 0.48f);

                    av.HairPath = "Avatar/Hair/MidFringe/MidFringe";
                    av.HairColor = new Color32(72, 58, 48, 255);

                    av.WithFaceLayer("Avatar/Layers/Face/Face_SmugPout", new Color(0.12f, 0.10f, 0.09f));
                    av.WithFaceLayer("Avatar/Layers/Face/TiredEyes", new Color(0.40f, 0.22f, 0.24f, 0.80f));
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Stubble", new Color32(55, 45, 38, 255));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color32(72, 42, 98, 255));
                    av.WithBodyLayer("Avatar/Layers/Bottom/CargoPants", new Color32(48, 52, 46, 255));
                    av.WithBodyLayer("Avatar/Layers/Accessories/FingerlessGloves", new Color32(28, 28, 30, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/Cap/Cap", new Color32(22, 24, 28, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Head/Oakleys/Oakleys", new Color32(18, 20, 22, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color32(92, 58, 38, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color32(38, 40, 44, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Neck/GoldChain/GoldChain", new Color32(212, 168, 58, 255));
                })
                .EnsureSupplier()
                .WithSupplierDefaults(s =>
                {
                    s.WithPersistentId(SupplierPersistentId)
                     .WithOrderLimits(80f, 5000f)
                     .WithStashDeadDrop<BehindAutoShop>()
                     .WithDeliveryItem(VapeCartProduct.ProductId)
                     .WithRecommendationMessage("Know a guy who sells carts. Goes by Fog. Gave him your number.")
                     .WithUnlockHint("Remy Fogarty unlocked. Order carts at the Town Hall fountain.");
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(1.8f)
                     .SetUnlocked(false)
                     .SetUnlockType(NPCRelationship.UnlockType.Recommendation)
                     .WithConnectionsById(new string[] { "jeff_gilmore" });
                })
                .WithSchedule(plan =>
                {
                    if (fallback == null) return;
                    plan.StayInBuilding(motel ?? fallback, 0, 480, null, "Crash");
                    plan.StayInBuilding(gasMart ?? fallback, 800, 90, null, "Munchies");
                    plan.StayInBuilding(pawnShop ?? fallback, 930, 120, null, "Pawn");
                    plan.StayInBuilding(cafe ?? fallback, 1130, 120, null, "Cafe");
                    plan.UseATM(1330, null, "ATM");
                    plan.StayInBuilding(arcade ?? fallback, 1400, 180, null, "Arcade");
                    plan.StayInBuilding(budsBar ?? fallback, 1700, 180, null, "Bar");
                    plan.StayInBuilding(nightclub ?? fallback, 2000, 240, null, "Club");
                });

            MelonLogger.Msg("[Remy] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.05f;
                Region = Region.Downtown;

                Appearance.Build();
                Schedule.Enable();

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

                MelonLogger.Msg("[Remy] Remy Fogarty loaded as vape-cart supplier.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Remy] OnCreated failed: " + ex);
            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (Dialogue != null)
                {
                    // If confrontation is active, arm confrontation dialogue instead of normal intro
                    if (SnitchStoryManager.IsConfrontRemyActive)
                    {
                        RemyConfrontationDialogue.ArmConfrontationDialogue();
                        yield break;
                    }

                    if (RemyDialogue.TryRegisterAndArm(Dialogue))
                    {
                        MelonLogger.Msg("[Remy] Cart supplier intro is ready.");
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator ConfigureExistingDeadDropAfterLoad()
        {
            for (int attempt = 0; attempt < 80; attempt++)
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
                DeadDropInstance selectedDrop = DeadDropManager.Get<TownHallFountain>();
                if (selectedDrop == null) return false;

                string guid = selectedDrop.GUID;
                if (string.IsNullOrWhiteSpace(guid)) return false;

                var nativeDrops = DeadDrop.DeadDrops;
                if (nativeDrops == null || nativeDrops.Count == 0) return false;

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop = nativeDrops[i];
                    if (drop == null) continue;

                    bool match = string.Equals(drop.GUID.ToString(), guid, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(drop.BakedGUID, guid, StringComparison.OrdinalIgnoreCase);

                    if (!match) continue;

                    drop.DeadDropName = PaymentDropName;
                    drop.DeadDropDescription = PaymentDropDescription;

                    MelonLogger.Msg("[Remy] Fountain dead drop claimed: " + guid);
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}