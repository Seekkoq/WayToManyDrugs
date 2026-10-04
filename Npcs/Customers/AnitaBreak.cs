using System;
using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using S1API.GameTime;
using S1API.Map;
using S1API.Properties;
using S1API.Properties.Interfaces;
using UnityEngine;
using S1API.Entities.Voices;

using S1DrugType = S1API.Products.DrugType;
using S1Region = S1API.Map.Region;

namespace CustomNPCExample.NPCs
{
    public sealed class AnitaBreak : NPC
    {
        public static AnitaBreak Instance { get; private set; }

        public const string NpcId = "custom_anita_break";
        public const string ConnectionNpcId = "kathy_henderson";

        private const string HomeBuildingName = "North apartments";

        public static Vector3? SpawnPositionOverride { get; set; }
        public static float SpawnYaw { get; set; } = 45f;

        private bool _unlockEventHooked;

        public override bool IsPhysical => true;
        public override bool IsCustomer => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[Anita Break] ConfigurePrefab started.");

            Building home =
                NPCBuildingLookup.Get(HomeBuildingName);

            Building cafe =
                NPCBuildingLookup.Get("Cafe");

            Building supermarket =
                NPCBuildingLookup.Get("Supermarket");

            Building pawnShop =
                NPCBuildingLookup.Get("Pawn Shop");

            Building nightclub =
                NPCBuildingLookup.Get("Nightclub");

            LogMissingBuilding(home, HomeBuildingName);
            LogMissingBuilding(cafe, "Cafe");
            LogMissingBuilding(supermarket, "Supermarket");
            LogMissingBuilding(pawnShop, "Pawn Shop");
            LogMissingBuilding(nightclub, "Nightclub");

            Building fallback =
                home ?? supermarket ?? cafe ?? pawnShop ?? nightclub;

            if (SpawnPositionOverride.HasValue)
            {
                builder.WithSpawnPosition(
                    SpawnPositionOverride.Value,
                    Quaternion.Euler(0f, SpawnYaw, 0f)
                );
            }

            builder
                .WithIdentity(NpcId, "Anita", "Break")
                .WithVoice(NPCVoiceCatalog.Female2, 1.0f)
                .WithRegion(S1Region.Northtown)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 1f;
                    av.Height = 0.97f;
                    av.Weight = 0.54f;

                    av.SkinColor = new Color32(220, 178, 152, 255);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(0.92f, 0.91f, 0.88f);
                    av.PupilDilation = 0.55f;

                    av.EyebrowScale = 0.97f;
                    av.EyebrowThickness = 0.90f;
                    av.EyebrowRestingHeight = -0.03f;
                    av.EyebrowRestingAngle = 2f;

                    av.LeftEye =
                        new ValueTuple<float, float>(0.38f, 0.52f);

                    av.RightEye =
                        new ValueTuple<float, float>(0.40f, 0.52f);

                    av.HairPath =
                        "Avatar/Hair/MessyBob/MessyBob";

                    av.HairColor = new Color32(96, 46, 30, 255);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SlightFrown",
                        new Color(0.18f, 0.10f, 0.09f, 1f)
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/TiredEyes",
                        new Color(0.36f, 0.19f, 0.20f, 0.50f)
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Freckles",
                        new Color32(157, 91, 64, 110)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/FlannelButtonUp",
                        new Color32(66, 114, 123, 255)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color32(52, 62, 79, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                        new Color32(172, 88, 49, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color32(217, 210, 191, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color32(70, 48, 33, 255)
                    );
                })
                .WithCustomerDefaults(customer =>
                {
                    customer
                        .WithSpending(340f, 880f)
                        .WithOrdersPerWeek(5, 6)
                        .WithPreferredOrderDay(Day.Thursday)
                        .WithOrderTime(1830)
                        .WithStandards(CustomerStandard.Moderate)
                        .AllowDirectApproach(true)
                        .GuaranteeFirstSample(true)
                        .WithMutualRelationRequirement(1.5f, 3.5f)
                        .WithCallPoliceChance(0.03f)
                        .WithDependence(0.35f, 1.15f)
                        .WithAffinities(
                            new ValueTuple<S1DrugType, float>[]
                            {
                                new ValueTuple<S1DrugType, float>(
                                    S1DrugType.MDMA,
                                    5f
                                ),

                                new ValueTuple<S1DrugType, float>(
                                    S1DrugType.Shrooms,
                                    1.2f
                                ),

                                new ValueTuple<S1DrugType, float>(
                                    S1DrugType.Marijuana,
                                    0.8f
                                ),

                                new ValueTuple<S1DrugType, float>(
                                    S1DrugType.Cocaine,
                                    -0.3f
                                ),

                                new ValueTuple<S1DrugType, float>(
                                    S1DrugType.Methamphetamine,
                                    -0.5f
                                )
                            }
                        )
                        .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Euphoric,
                                Property.Energizing
                            }
                        );
                })
                .WithRelationshipDefaults(relationship =>
                {
                    relationship
                        .WithDelta(1.1f)
                        .SetUnlocked(false)
                        .SetUnlockType(
                            NPCRelationship.UnlockType.DirectApproach
                        )
                        .WithConnectionsById(
                            new[] { ConnectionNpcId }
                        );
                })
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {

                        return;
                    }

                    plan.StayInBuilding(
                        home ?? fallback,
                        0, 385, null,
                        "Anita - Five more minutes"
                    );

                    plan.StayInBuilding(
                        cafe ?? fallback,
                        625, 45, null,
                        "Anita - First coffee"
                    );

                    plan.StayInBuilding(
                        supermarket ?? fallback,
                        715, 375, null,
                        "Anita - Covering another shift"
                    );

                    NpcScheduleMixins.AtmStop(
                        plan, 1355, "Anita - lunch cash");

                    plan.StayInBuilding(
                        pawnShop ?? fallback,
                        1410, 55, null,
                        "Anita - Stretch the paycheck"
                    );

                    plan.StayInBuilding(
                        cafe ?? fallback,
                        1510, 150, null,
                        "Anita - The actual break"
                    );

                    plan.StayInBuilding(
                        supermarket ?? fallback,
                        1740, 90, null,
                        "Anita - They need help again"
                    );

                    plan.StayInBuilding(
                        nightclub ?? fallback,
                        1910, 180, null,
                        "Anita - Not answering work calls"
                    );

                    plan.StayInBuilding(
                        home ?? fallback,
                        2210, 115, null,
                        "Anita - Alarm set too early"
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Anita Break] Complete daily schedule configured."
                    );
                });

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Anita Break] ConfigurePrefab completed."
            );
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;

                Appearance.Build();
                Aggressiveness = 0.06f;
                Region = S1Region.Northtown;

                Relationship.OnUnlocked -= HandleUnlocked;
                Relationship.OnUnlocked += HandleUnlocked;
                _unlockEventHooked = true;

                if (Schedule != null)
                    Schedule.Enable();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Anita Break] Loaded as a Northtown customer."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Anita Break] OnCreated failed: " + ex
                );
            }
        }

        private void HandleUnlocked(
            NPCRelationship.UnlockType unlockType,
            bool notify)
        {
            if (!notify)
                return;
            }

        protected override void OnDestroyed()
        {
            try
            {
                if (_unlockEventHooked && Relationship != null)
                    Relationship.OnUnlocked -= HandleUnlocked;
            }
            catch (Exception)
            {

            }
            finally
            {
                _unlockEventHooked = false;

                if (ReferenceEquals(Instance, this))
                    Instance = null;

                base.OnDestroyed();
            }
        }

        private static void LogMissingBuilding(
            Building building,
            string name)
        {
            if (building == null)
            {

            }
        }
    }
}
