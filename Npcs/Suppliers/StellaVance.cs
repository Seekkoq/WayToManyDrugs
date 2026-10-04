using MelonLoader;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using Il2CppScheduleOne.NPCs.Relation;
using S1API.Map;
using System;
using System.Collections;
using UnityEngine;
using S1API.Entities.Voices;

namespace CustomNPCExample.NPCs
{
    public sealed class StellaVance : NPC
    {
        public static StellaVance Instance { get; private set; }

        public const string NpcId = "custom_stella_vance";
        public const string SupplierPersistentId = "wvc_stella_vance_supplier_v1";

        private bool _dialogueSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[Stella] ConfigurePrefab started.");

            Building northApartments = NPCBuildingLookup.Get("North apartments");
            Building gasMart         = NPCBuildingLookup.Get("West Gas-Mart");
            Building westApartments  = NPCBuildingLookup.Get("Apartment Building");
            Building supermarket     = NPCBuildingLookup.Get("Supermarket");
            Building slopShop        = NPCBuildingLookup.Get("Slop Shop");
            Building cafe            = NPCBuildingLookup.Get("Cafe");
            Building pawnShop        = NPCBuildingLookup.Get("Pawn Shop");
            Building budsBar         = NPCBuildingLookup.Get("Bud's Bar");

            Building fallback = gasMart ?? westApartments ?? northApartments ?? supermarket ?? slopShop ?? cafe ?? budsBar;

            builder
                .WithIdentity(NpcId, "Stella", "Vance")
                .WithVoice(NPCVoiceCatalog.Female2, 1.0f)
                .WithRegion(Region.Suburbia)
                .WithSpawnPosition(
                    new Vector3(-25f, -3.57f, 128f),
                    Quaternion.Euler(0f, 90f, 0f))
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 1f;
                    av.Height = 0.96f;
                    av.Weight = 0.62f;

                    av.SkinColor = new Color32(220, 185, 155, 255);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(0.88f, 0.86f, 0.84f);
                    av.PupilDilation = 0.50f;

                    av.EyebrowScale = 1.05f;
                    av.EyebrowThickness = 1.10f;
                    av.EyebrowRestingHeight = -0.05f;
                    av.EyebrowRestingAngle = -3f;

                    av.LeftEye  = new ValueTuple<float, float>(0.46f, 0.50f);
                    av.RightEye = new ValueTuple<float, float>(0.46f, 0.50f);

                    av.HairPath  = "Avatar/Hair/LowBun/LowBun";
                    av.HairColor = new Color32(165, 125, 88, 255);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SlightSmile",
                        new Color(0.12f, 0.10f, 0.09f));
                    av.WithFaceLayer(
                        "Avatar/Layers/Face/OldPersonWrinkles",
                        new Color(0.28f, 0.20f, 0.16f, 0.40f));

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color32(245, 240, 230, 255));
                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color32(52, 68, 88, 255));

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Apron/Apron",
                        new Color32(232, 218, 196, 255));
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Flats/Flats",
                        new Color32(88, 68, 52, 255));
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/SmallRoundGlasses/SmallRoundGlasses",
                        new Color32(42, 38, 34, 255));
                })
                .WithRelationshipDefaults(r => r
                    .WithDelta(1.6f)
                    // Gated like the other suppliers: she only shows up on the phone once one of her
                    // connections passes her number on. Meg Cooley is the one who vouches for her
                    // cookies, and she is in Westville where Stella trades.
                    .SetUnlocked(false)
                    .SetUnlockType(NPCRelationship.UnlockType.Recommendation)
                    .WithConnectionsById(new[]
                    {
                        "meg_cooley"
                    }))
                .EnsureSupplier()
                .WithSupplierDefaults(s => s
                    .WithPersistentId(SupplierPersistentId)
                    .WithOrderLimits(50f, 2500f)
                    .WithStashDeadDrop<S1API.DeadDrops.Native.PawnShopWestWall>()
                    .WithDeliveryItem(
                        CustomNPCExample.Products.Edibles.CookieIngredients.CannabisFlourId)
                    .WithDeliveryItem(
                        CustomNPCExample.Products.Edibles.CookieIngredients.ButterscotchChipsId))
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {

                        return;
                    }

                    // Trade hours start the moment the player wakes up, so a morning run never ends
                    // at an empty counter. She works out of Westville, where Meg Cooley is.
                    plan.StayInBuilding(gasMart ?? westApartments ?? northApartments ?? fallback, 0, 1380, null, "Trade Hours");
                    plan.StayInBuilding(westApartments ?? fallback, 1380, 60, null, "Home");

                    global::CustomNPCExample.Utils.WvcLog.Msg("[Stella] Trade-hours schedule assigned (Westville).");
                });

            global::CustomNPCExample.Utils.WvcLog.Msg("[Stella] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.03f;
                Region = Region.Westville;

                Appearance.Build();
                Schedule.Enable();

                EnforceConnectionGating();

                if (!_dialogueSetupStarted)
                {
                    _dialogueSetupStarted = true;
                    MelonCoroutines.Start(ConfigureDialogueAfterLoad());
                }

                global::CustomNPCExample.Utils.WvcLog.Msg("[Stella] Stella Vance loaded.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Stella] OnCreated failed: " + ex);
            }
        }

        /// <summary>
        /// Saves made while she was unlocked on sight still carry that flag. Clearing it puts her
        /// back behind her connection, so it is the recommendation that actually unlocks her.
        /// </summary>
        private void EnforceConnectionGating()
        {
            try
            {
                Il2CppScheduleOne.NPCs.NPC native = ResolveNativeNpc();

                if (native == null)
                {

                    return;
                }

                NPCRelationData data = native.RelationData;

                if (data == null)
                    return;

                if (!data._Unlocked_k__BackingField)
                    return;

                if (data._UnlockType_k__BackingField == NPCRelationData.EUnlockType.Recommendation)
                    return;

                data._Unlocked_k__BackingField = false;
                data._UnlockType_k__BackingField = NPCRelationData.EUnlockType.Recommendation;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Stella] Re-locked: her connection is what puts her on the phone now.");
            }
            catch (Exception)
            {

            }
        }

        private Il2CppScheduleOne.NPCs.NPC ResolveNativeNpc()
        {
            try
            {
                var native = gameObject.GetComponent<Il2CppScheduleOne.NPCs.NPC>();

                if (native != null)
                    return native;
            }
            catch
            {
            }

            try
            {
                var child = gameObject.GetComponentInChildren<Il2CppScheduleOne.NPCs.NPC>(true);

                if (child != null)
                    return child;
            }
            catch
            {
            }

            try
            {
                return gameObject.GetComponentInParent<Il2CppScheduleOne.NPCs.NPC>();
            }
            catch
            {
                return null;
            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (Dialogue != null && StellaVanceDialogue.TryRegisterAndArm(Dialogue))
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg("[Stella] Cookie supplier intro is ready.");
                    yield break;
                }
                yield return new WaitForSeconds(0.25f);
            }

        }
    }
}
