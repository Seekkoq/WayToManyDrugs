using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using S1API.GameTime;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using UnityEngine;
using S1API.Entities.Voices;

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
            global::CustomNPCExample.Utils.WvcLog.Msg("[Combo] ConfigurePrefab started.");

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
                .WithVoice(NPCVoiceCatalog.Cold, 0.95f)
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

                    av.Gender = 0f;

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

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Goatee",
                        new Color(
                            0.08f,
                            0.045f,
                            0.025f,
                            1f
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color(
                            0.94f,
                            0.94f,
                            0.90f,
                            1f
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(
                            0.09f,
                            0.10f,
                            0.13f,
                            1f
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                        new Color(
                            0.88f,
                            0.86f,
                            0.78f,
                            1f
                        )
                    );

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
                                Property.Disorienting,
                                Property.Energizing
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
                             JessePinkman.NpcId
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
                        northApartments ?? fallback,
                        0,
                        375,
                        null,
                        "Crash"
                    );

                    plan.StayInBuilding(
                        motel ?? fallback,
                        645,
                        105,
                        null,
                        "Gas-Mart"
                    );

                    plan.StayInBuilding(
                        shootingRange ?? fallback,
                        910,
                        180,
                        null,
                        "Pawn Shop"
                    );

                    plan.StayInBuilding(
                        arcade ?? fallback,
                        1150,
                        180,
                        null,
                        "Arcade"
                    );

                    NpcScheduleMixins.VendingStop(
                        plan, 1435, "Combo - vending machine snack");

                    plan.StayInBuilding(
                        budsBar ?? fallback,
                        1530,
                        180,
                        null,
                        "Bud's Bar"
                    );

                    plan.StayInBuilding(
                        nightclub ?? fallback,
                        1830,
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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Combo] Complete 24-hour schedule assigned."
                    );
                });

            global::CustomNPCExample.Utils.WvcLog.Msg("[Combo] ConfigurePrefab completed.");
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
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


        }
    }
}
