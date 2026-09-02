    using System;
    using MelonLoader;
    using S1API.Economy;
    using S1API.Entities;
    using S1API.Entities.Customer;
    using S1API.Entities.Relation;
    using S1API.Entities.Schedule;
    using S1API.GameTime;
    using S1API.Map;
    using S1API.Map.Buildings;
    using S1API.Products;
    using S1API.Properties;
    using S1API.Properties.Interfaces;
    using UnityEngine;

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
                MelonLogger.Msg(
                    "[CrystalMethany] ConfigurePrefab started."
                );

                // Buildings already used elsewhere in your mod.
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

                /*
                 * Ensures the schedule remains complete if one specific
                 * building lookup fails. The missing stop falls back to
                 * whichever valid building was found first.
                 */
                Building fallback =
                    apartments
                    ?? motel
                    ?? gasMart
                    ?? pawnShop
                    ?? chinese
                    ?? arcade
                    ?? shootingRange
                    ?? nightclub;

                /*
                 * Temporary Westville spawn.
                 * Change with an F8 position later if desired.
                 */
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
                        /*
                         * Icy, restless nightclub look.
                         * All paths are taken from your working dump.
                         */
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

                        // Distinct long, cold-blue hair.
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

                        // Dark sleeveless top.
                        av.WithBodyLayer(
                            "Avatar/Layers/Top/V-Neck",
                            new Color(
                                0.12f,
                                0.05f,
                                0.18f,
                                1f
                            )
                        );

                        // Black cargo pants.
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

                        // Web tattoo fits the edgy nighttime appearance.
                        av.WithBodyLayer(
                            "Avatar/Layers/Tattoos/RightArm/RightArm_Web",
                            new Color(
                                0.08f,
                                0.18f,
                                0.25f,
                                1f
                            )
                        );

                        // Light vest gives her a unique silhouette.
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
                                  Property.Energizing,
                                  Property.AntiGravity,
                                  Property.Paranoia
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

                         /*
                          * Valid native connection from your list.
                          */
                         .WithConnectionsById(
                             new[]
                             {
                                 "austin_steiner"
                             }
                         );
                    })
                    .WithSchedule(plan =>
                    {
                        if (fallback == null)
                        {
                            MelonLogger.Warning(
                                "[CrystalMethany] No valid schedule " +
                                "buildings were found."
                            );

                            return;
                        }

                        /*
                         * Complete 24-hour schedule.
                         *
                         * 00:00-06:00 = 360 minutes
                         * 06:00-08:00 = 120 minutes
                         * 08:00-11:00 = 180 minutes
                         * 11:00-13:00 = 120 minutes
                         * 13:00-16:00 = 180 minutes
                         * 16:00-18:00 = 120 minutes
                         * 18:00-22:00 = 240 minutes
                         * 22:00-24:00 = 120 minutes
                         *
                         * Total = 1,440 minutes. No gaps.
                         */

                        plan.StayInBuilding(
                            motel ?? fallback,
                            0,
                            360,
                            null,
                            "Motel"
                        );

                        plan.StayInBuilding(
    arcade ?? fallback,
    360,
    640,
    null,
    "arcade"
);


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

                        MelonLogger.Msg(
                            "[CrystalMethany] Complete 24-hour " +
                            "schedule assigned."
                        );
                    });

                MelonLogger.Msg(
                    "[CrystalMethany] ConfigurePrefab completed."
                );
            }

            protected override void OnCreated()
            {

            try
            {
                Schedule.Enable();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[CrystalMethany] Schedule failed to enable: " + ex.Message);
            }
            try
                {
                    base.OnCreated();

                    Appearance.Build();

                    Aggressiveness = 0.12f;
                    Region = Region.Westville;

                    Schedule.Enable();

                    Instance = this;

                    MelonLogger.Msg(
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

                MelonLogger.Warning(
                    "[CrystalMethany] Missing building: " +
                    buildingName
                );
            }
        }
    }