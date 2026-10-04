    using System;
    using MelonLoader;
    using S1API.Economy;
    using S1API.Entities;
    using S1API.Entities.Customer;
    using S1API.Entities.Relation;
    using S1API.Entities.Schedule;
    using S1API.Entities.Voices;
    using S1API.GameTime;
    using S1API.Map;
    using S1API.Map.Buildings;
    using S1API.Products;
    using S1API.Properties;
    using S1API.Properties.Interfaces;
    using UnityEngine;
    using S1API.Entities.Voices;

    namespace CustomNPCExample.NPCs
    {
        public sealed class CrystalMethany : NPC
        {
            public static CrystalMethany Instance
            {
                get;
                private set;
            }

            public const string NpcId =
                "custom_crystal_methany";

            public override bool IsPhysical => true;
            public override bool IsCustomer => true;

            protected override void ConfigurePrefab(
                NPCPrefabBuilder builder)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[CrystalMethany] ConfigurePrefab started."
                );

                Building motel =
                    NPCBuildingLookup.Get("Motel Office");

                Building gasMart =
                    NPCBuildingLookup.Get("West Gas-Mart");

                Building pawnShop =
                    NPCBuildingLookup.Get("Pawn Shop");

                Building chinese =
                    NPCBuildingLookup.Get("Chinese Restaurant");

                Building arcade =
                    NPCBuildingLookup.Get("Arcade");

                Building shootingRange =
                    NPCBuildingLookup.Get("Shooting Range");

                Building nightclub =
                    NPCBuildingLookup.Get("Nightclub");

                Building apartments =
                    NPCBuildingLookup.Get("North apartments");

                LogMissingBuilding(
                    motel,
                    "Motel Office"
                );

                LogMissingBuilding(
                    gasMart,
                    "West Gas-Mart"
                );

                LogMissingBuilding(
                    pawnShop,
                    "Pawn Shop"
                );

                LogMissingBuilding(
                    chinese,
                    "Chinese Restaurant"
                );

                LogMissingBuilding(
                    arcade,
                    "Arcade"
                );

                LogMissingBuilding(
                    shootingRange,
                    "Shooting Range"
                );

                LogMissingBuilding(
                    nightclub,
                    "Nightclub"
                );

                LogMissingBuilding(
                    apartments,
                    "North apartments"
                );

                Building fallback =
                    apartments
                    ?? motel
                    ?? gasMart
                    ?? pawnShop
                    ?? chinese
                    ?? arcade
                    ?? shootingRange
                    ?? nightclub;

                Vector3 spawnPosition =
                    new Vector3(
                        -38.5f,
                        0.24f,
                        83.0f
                    );

                builder
                    .WithIdentity(
                        NpcId,
                        "Crystal",
                        "Methany"
                    )
                    .WithVoice(NPCVoiceCatalog.Crackhead, 1.0f)
                    .WithRegion(
                        Region.Westville
                    )
                    .WithSpawnPosition(
                        spawnPosition,
                        Quaternion.Euler(
                            0f,
                            145f,
                            0f
                        )
                    )
                    .WithAppearanceDefaults(av =>
                    {
                        av.Gender = 1f;
                        av.Height = 1.02f;
                        av.Weight = 0.40f;

                        av.SkinColor =
                            new Color(
                                0.80f,
                                0.67f,
                                0.60f,
                                1f
                            );

                        av.LeftEyeLidColor =
                            av.SkinColor;

                        av.RightEyeLidColor =
                            av.SkinColor;

                        av.EyeBallTint =
                            new Color(
                                0.62f,
                                0.86f,
                                0.96f,
                                1f
                            );

                        av.PupilDilation = 0.88f;

                        av.EyebrowScale = 0.90f;
                        av.EyebrowThickness = 0.78f;
                        av.EyebrowRestingHeight = 0.10f;
                        av.EyebrowRestingAngle = -7f;

                        av.LeftEye =
                            new ValueTuple<float, float>(
                                0.40f,
                                0.47f
                            );

                        av.RightEye =
                            new ValueTuple<float, float>(
                                0.40f,
                                0.47f
                            );

                        av.HairPath =
                            "Avatar/Hair/LongSlicked/LongSlicked";

                        av.HairColor =
                            new Color(
                                0.30f,
                                0.48f,
                                0.58f,
                                1f
                            );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Face_SmugPout",
                            new Color(
                                0.12f,
                                0.08f,
                                0.10f,
                                1f
                            )
                        );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/EyeShadow",
                            new Color(
                                0.18f,
                                0.07f,
                                0.24f,
                                0.85f
                            )
                        );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/TiredEyes",
                            new Color(
                                0.34f,
                                0.18f,
                                0.26f,
                                0.75f
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Top/V-Neck",
                            new Color(
                                0.12f,
                                0.05f,
                                0.18f,
                                1f
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Bottom/CargoPants",
                            new Color(
                                0.045f,
                                0.055f,
                                0.065f,
                                1f
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Accessories/FingerlessGloves",
                            new Color(
                                0.07f,
                                0.08f,
                                0.09f,
                                1f
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Tattoos/RightArm/RightArm_Web",
                            new Color(
                                0.08f,
                                0.18f,
                                0.25f,
                                1f
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Chest/OpenVest/OpenVest",
                            new Color(
                                0.58f,
                                0.72f,
                                0.78f,
                                1f
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Head/Oakleys/Oakleys",
                            new Color(
                                0.08f,
                                0.16f,
                                0.22f,
                                1f
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Feet/CombatBoots/CombatBoots",
                            new Color(
                                0.035f,
                                0.035f,
                                0.045f,
                                1f
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Waist/Belt/Belt",
                            new Color(
                                0.15f,
                                0.10f,
                                0.08f,
                                1f
                            )
                        );
                    })
                    .WithCustomerDefaults(cd =>
                    {
                        cd.WithSpending(
                              400f,
                              1200f
                          )
                          .WithOrdersPerWeek(
                              4,
                              7
                          )
                          .WithPreferredOrderDay(
                              Day.Monday
                          )
                          .WithOrderTime(
                              2230
                          )
                          .WithStandards(
                              CustomerStandard.VeryLow
                          )
                          .AllowDirectApproach(
                              true
                          )
                          .GuaranteeFirstSample(
                              false
                          )
                          .WithMutualRelationRequirement(
                              2f,
                              5f
                          )
                          .WithCallPoliceChance(
                              0f
                          )
                          .WithDependence(
                              0.90f,
                              2.10f
                          )
                          .WithAffinities(
                              new ValueTuple<DrugType, float>[]
                              {
                                  new ValueTuple<DrugType, float>(
                                      DrugType.Methamphetamine,
                                      1.00f
                                  ),

                                  new ValueTuple<DrugType, float>(
                                      DrugType.Cocaine,
                                      0.70f
                                  ),

                                  new ValueTuple<DrugType, float>(
                                      DrugType.Marijuana,
                                      -0.15f
                                  ),

                                  new ValueTuple<DrugType, float>(
                                      DrugType.Shrooms,
                                      -0.80f
                                  )
                              }
                          )
                          .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Foggy,
                                Property.Disorienting
                            }
                        );
                    })
                    .WithRelationshipDefaults(r =>
                    {
                        r.WithDelta(
                             1.5f
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
                                 "dean_webster"
                             }
                         );
                    })
                    .WithSchedule(plan =>
                    {
                        if (fallback == null)
                        {


                            return;
                        }

                        plan.StayInBuilding(
                            motel ?? fallback,
                            0,
                            345,
                            null,
                            "Motel"
                        );

                        plan.StayInBuilding(
    arcade ?? fallback,
    380,
    400,
    null,
    "arcade"
);

                        NpcScheduleMixins.SitStop(
                            plan, 800, 40, "Crystal - sits outside the arcade");

                        plan.StayInBuilding(
                            chinese ?? fallback,
                            1100,
                            120,
                            null,
                            "Lunch"
                        );

                        plan.StayInBuilding(
                            arcade ?? fallback,
                            1300,
                            180,
                            null,
                            "Arcade"
                        );

                        NpcScheduleMixins.VendingStop(
                            plan, 1545, "Crystal - vending machine");

                        plan.StayInBuilding(
                            shootingRange ?? fallback,
                            1600,
                            120,
                            null,
                            "Shooting Range"
                        );

                        plan.StayInBuilding(
                            nightclub ?? fallback,
                            1800,
                            240,
                            null,
                            "Nightclub"
                        );

                        plan.StayInBuilding(
                            apartments ?? fallback,
                            2200,
                            120,
                            null,
                            "Home"
                        );

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[CrystalMethany] Complete 24-hour " +
                            "schedule assigned."
                        );
                    });

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[CrystalMethany] ConfigurePrefab completed."
                );
            }

            protected override void OnCreated()
            {

            try
            {
                Schedule.Enable();
            }
            catch (Exception)
            {

            }
            try
                {
                    base.OnCreated();

                    Appearance.Build();

                    Aggressiveness = 0.12f;
                    Region = Region.Westville;

                    Schedule.Enable();

                    Instance = this;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[CrystalMethany] Crystal Methany loaded " +
                        "in Westville."
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Error(
                        "[CrystalMethany] OnCreated failed: " +
                        ex
                    );
                }
            }

            private static void LogMissingBuilding(
                Building building,
                string buildingName)
            {
                if (building != null)
                    return;


            }
        }
    }
