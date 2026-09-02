using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.GameTime;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class ComboOrtega : NPC
    {
        public static ComboOrtega Instance { get; private set; }

        public const string NpcId =
            "custom_combo_ortega";

        public override bool IsPhysical => true;
        public override bool IsCustomer => true;

        protected override void ConfigurePrefab(
            NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Combo] ConfigurePrefab started.");

            Building motel =
                NPCBuildingLookup.Get("Motel Office");

            Building gasMart =
                NPCBuildingLookup.Get("West Gas-Mart");

            Building pawnShop =
                NPCBuildingLookup.Get("Pawn Shop");

            Building arcade =
                NPCBuildingLookup.Get("Arcade");

            Building budsBar =
                NPCBuildingLookup.Get("Bud's Bar");

            Building nightclub =
                NPCBuildingLookup.Get("Nightclub");

            Building northApartments =
                NPCBuildingLookup.Get("North apartments");

            Building hardware = NPCBuildingLookup.Get("Dan's Hardware Upstairs");
            Building shootingRange = NPCBuildingLookup.Get("Shooting Range");

            LogMissingBuilding(motel, "Motel Office");
            LogMissingBuilding(gasMart, "West Gas-Mart");
            LogMissingBuilding(pawnShop, "Pawn Shop");
            LogMissingBuilding(arcade, "Arcade");
            LogMissingBuilding(budsBar, "Bud's Bar");
            LogMissingBuilding(nightclub, "Nightclub");
            LogMissingBuilding(northApartments, "North apartments");


            Building fallback =
                motel ??
                gasMart ??
                pawnShop ??
                arcade ??
                budsBar ??
                nightclub ??
                shootingRange ??
                northApartments;

            Vector3 spawnPosition =
                new Vector3(
                    -55.8f,
                    -0.76f,
                    -44.2f
                );

            builder
                .WithIdentity(
                    NpcId,
                    "Combo",
                    "Ortega"
                )
                .WithRegion(
                    Region.Westville
                )
                .WithSpawnPosition(
                    spawnPosition,
                    Quaternion.Euler(
                        0f,
                        120f,
                        0f
                    )
                )
                .WithAppearanceDefaults(av =>
                {
                    /*
                     * Combo-inspired:
                     * heavy build, bald, black shades, white shirt,
                     * off-white jacket/hoodie approximation.
                     */

                    av.Gender = 0f;

                    // Shorter but heavy.
                    av.Height = 0.96f;
                    av.Weight = 1.15f;

                    av.SkinColor =
                        new Color(
                            0.76f,
                            0.58f,
                            0.46f,
                            1f
                        );

                    av.LeftEyeLidColor =
                        av.SkinColor;

                    av.RightEyeLidColor =
                        av.SkinColor;

                    av.EyeBallTint =
                        new Color(
                            0.82f,
                            0.84f,
                            0.82f,
                            1f
                        );

                    av.PupilDilation = 0.50f;

                    av.EyebrowScale = 0.95f;
                    av.EyebrowThickness = 0.95f;
                    av.EyebrowRestingHeight = -0.08f;
                    av.EyebrowRestingAngle = -3f;

                    /*
                     * Bald. If you want a tiny bit of shaved hair instead,
                     * use:
                     * Avatar/Hair/CloseBuzzCut/CloseBuzzCut
                     */
                    av.HairPath = string.Empty;

                    av.HairColor =
                        new Color(
                            0.04f,
                            0.03f,
                            0.02f,
                            1f
                        );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_NeutralPout",
                        new Color(
                            0.10f,
                            0.08f,
                            0.07f,
                            1f
                        )
                    );

                    // Goatee like the reference image.
                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Goatee",
                        new Color(
                            0.08f,
                            0.045f,
                            0.025f,
                            1f
                        )
                    );

                    // White shirt.
                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color(
                            0.94f,
                            0.94f,
                            0.90f,
                            1f
                        )
                    );

                    // Dark jeans.
                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(
                            0.09f,
                            0.10f,
                            0.13f,
                            1f
                        )
                    );

                    /*
                     * There is no hoodie in your dump, so this is the
                     * closest off-white jacket/hoodie silhouette.
                     */
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                        new Color(
                            0.88f,
                            0.86f,
                            0.78f,
                            1f
                        )
                    );

                    // Crystal-style sunglasses / Oakleys.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/Oakleys/Oakleys",
                        new Color(
                            0.015f,
                            0.015f,
                            0.018f,
                            1f
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color(
                            0.12f,
                            0.10f,
                            0.08f,
                            1f
                        )
                    );

                    // Optional: belt to break up the white top/dark pants.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color(
                            0.08f,
                            0.055f,
                            0.035f,
                            1f
                        )
                    );
                })
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(
                          180f,
                          650f
                      )
                      .WithOrdersPerWeek(
                          3,
                          5
                      )
                      .WithPreferredOrderDay(
                          Day.Saturday
                      )
                      .WithOrderTime(
                          2100
                      )
                      .WithStandards(
                          CustomerStandard.VeryLow
                      )
                      .AllowDirectApproach(
                          true
                      )
                      .GuaranteeFirstSample(
                          true
                      )
                      .WithMutualRelationRequirement(
                          2f,
                          5f
                      )
                      .WithCallPoliceChance(
                          0.01f
                      )
                      .WithDependence(
                          0.65f,
                          1.65f
                      )
                      .WithAffinities(
                          new ValueTuple<DrugType, float>[]
                          {
                              new ValueTuple<DrugType, float>(
                                  DrugType.Methamphetamine,
                                  0.95f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Marijuana,
                                  0.55f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Cocaine,
                                  0.35f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Shrooms,
                                  -0.45f
                              )
                          }
                      )
                      .WithPreferredProperties(
                          new PropertyBase[]
                          {
                              Property.Energizing,
                              Property.Paranoia,
                              Property.Sneaky
                          }
                      );
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(
                         1.2f
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
                             /*
                              * Your custom Jesse ID.
                              * This makes Combo unlock through Jesse.
                              */
                             JessePinkman.NpcId
                         }
                     );
                })
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {
                        MelonLogger.Warning(
                            "[Combo] No valid buildings found. " +
                            "Schedule was not assigned."
                        );

                        return;
                    }

                    /*
                     * Full 24-hour schedule, no empty space.
                     *
                     * 00:00-07:00 Motel
                     * 07:00-09:00 Gas-Mart
                     * 09:00-12:00 Pawn Shop
                     * 12:00-15:00 Arcade
                     * 15:00-18:00 Bud's Bar
                     * 18:00-23:00 Nightclub
                     * 23:00-00:00 North apartments / fallback
                     */

                    plan.StayInBuilding(
                        northApartments ?? fallback,
                        0,
                        420,
                        null,
                        "Crash"
                    );

                    plan.StayInBuilding(
                        motel ?? fallback,
                        700,
                        120,
                        null,
                        "Gas-Mart"
                    );

                    plan.StayInBuilding(
                        shootingRange ?? fallback,
                        900,
                        180,
                        null,
                        "Pawn Shop"
                    );

                    plan.StayInBuilding(
                        arcade ?? fallback,
                        1200,
                        180,
                        null,
                        "Arcade"
                    );

                    plan.StayInBuilding(
                        budsBar ?? fallback,
                        1500,
                        180,
                        null,
                        "Bud's Bar"
                    );

                    plan.StayInBuilding(
                        nightclub ?? fallback,
                        1800,
                        300,
                        null,
                        "Nightclub"
                    );

                    plan.StayInBuilding(
                        northApartments ?? fallback,
                        2300,
                        60,
                        null,
                        "Late"
                    );

                    MelonLogger.Msg(
                        "[Combo] Complete 24-hour schedule assigned."
                    );
                });

            MelonLogger.Msg("[Combo] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Appearance.Build();

                Aggressiveness = 0.12f;
                Region = Region.Westville;

                Schedule.Enable();

                Instance = this;

                MelonLogger.Msg(
                    "[Combo] Combo Ortega loaded in Westville."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Combo] OnCreated failed: " +
                    ex
                );
            }
        }

        private static void LogMissingBuilding(
            Building building,
            string name)
        {
            if (building != null)
                return;

            MelonLogger.Warning(
                "[Combo] Missing building: " + name
            );
        }
    }
}