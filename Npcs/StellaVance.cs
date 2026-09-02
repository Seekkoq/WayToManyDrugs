using MelonLoader;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Map;
using System;
using System.Collections;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class StellaVance : NPC
    {
        public static StellaVance Instance { get; private set; }

        public const string NpcId = "custom_stella_vance";

        private bool _dialogueSetupStarted;

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Stella] ConfigurePrefab started.");

            Building northApartments = NPCBuildingLookup.Get("North apartments");
            Building supermarket     = NPCBuildingLookup.Get("Supermarket");
            Building slopShop        = NPCBuildingLookup.Get("Slop Shop");
            Building cafe            = NPCBuildingLookup.Get("Cafe");
            Building pawnShop        = NPCBuildingLookup.Get("Pawn Shop");
            Building budsBar         = NPCBuildingLookup.Get("Bud's Bar");

            Building fallback = northApartments ?? supermarket ?? slopShop ?? cafe ?? budsBar;

            builder
                .WithIdentity(NpcId, "Stella", "Vance")
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
                    .SetUnlocked(true))
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {
                        MelonLogger.Warning("[Stella] No valid schedule buildings found.");
                        return;
                    }

                    plan.StayInBuilding(northApartments ?? fallback,    0, 420, null, "Home");
                    plan.StayInBuilding(supermarket     ?? fallback,  700, 180, null, "Morning Shopping");
                    plan.StayInBuilding(slopShop        ?? fallback, 1000, 240, null, "Baking");
                    plan.StayInBuilding(cafe            ?? fallback, 1400, 180, null, "Break");
                    plan.StayInBuilding(pawnShop        ?? fallback, 1700, 120, null, "Errands");
                    plan.StayInBuilding(budsBar         ?? fallback, 1900, 180, null, "Evening");
                    plan.StayInBuilding(northApartments ?? fallback, 2200, 120, null, "Home Late");

                    MelonLogger.Msg("[Stella] Schedule assigned.");
                });

            MelonLogger.Msg("[Stella] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.03f;
                Region = Region.Suburbia;

                Appearance.Build();
                Schedule.Enable();

                if (!_dialogueSetupStarted)
                {
                    _dialogueSetupStarted = true;
                    MelonCoroutines.Start(ConfigureDialogueAfterLoad());
                }

                MelonLogger.Msg("[Stella] Stella Vance loaded.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Stella] OnCreated failed: " + ex);
            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (Dialogue != null && StellaVanceDialogue.TryRegisterAndArm(Dialogue))
                {
                    MelonLogger.Msg("[Stella] Cookie supplier intro is ready.");
                    yield break;
                }
                yield return new WaitForSeconds(0.25f);
            }
            MelonLogger.Warning("[Stella] Timed out waiting for dialogue components.");
        }
    }
}
