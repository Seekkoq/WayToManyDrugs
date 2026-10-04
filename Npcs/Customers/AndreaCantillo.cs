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
using System;
using UnityEngine;
using S1API.Entities.Voices;

namespace CustomNPCExample.NPCs
{
    public sealed class AndreaCantillo : NPC
    {
        public static AndreaCantillo Instance;

        public const string NpcId = "custom_andrea_cantillo";

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Building motel = NPCBuildingLookup.Get("Motel Office");
            Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
            Building pawnShop = NPCBuildingLookup.Get("Pawn Shop");
            Building chinese = NPCBuildingLookup.Get("Chinese Restaurant");
            Building arcade = NPCBuildingLookup.Get("Arcade");
            Building nightclub = NPCBuildingLookup.Get("Nightclub");
            Building northApartments = NPCBuildingLookup.Get("North apartments");
            Building bradstent = NPCBuildingLookup.Get("Brad's Tent");
            Building pillville = NPCBuildingLookup.Get("Pillville");

            Vector3 spawnPos = new Vector3(
                -35.958f,
                0.24f,
                83.044f
            );

            builder
                .WithIdentity(NpcId, "Andrea", "Cantillo")
                .WithVoice(NPCVoiceCatalog.Female2, 1.02f)
                .WithRegion(Region.Westville)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 1f;
                    av.Height = 0.93f;
                    av.Weight = 0.38f;

                    av.SkinColor = new Color32(
                        232,
                        210,
                        195,
                        255
                    );

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(
                        0.95f,
                        0.82f,
                        0.82f
                    );

                    av.PupilDilation = 0.92f;

                    av.EyebrowScale = 0.82f;
                    av.EyebrowThickness = 0.65f;
                    av.EyebrowRestingHeight = 0.12f;
                    av.EyebrowRestingAngle = -4f;

                    av.HairPath =
                        "Avatar/Hair/ShoulderLength/ShoulderLength";

                    av.HairColor = new Color32(
                        95,
                        45,
                        115,
                        255
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        new Color(0.15f, 0.12f, 0.11f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color32(135, 140, 148, 255)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color32(55, 60, 70, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color32(180, 180, 180, 255)
                    );
                })
                .WithSpawnPosition(spawnPos)
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(35f, 140f)
                      .WithOrdersPerWeek(5, 7)
                      .WithPreferredOrderDay(Day.Thursday)
                      .WithOrderTime(1730)
                      .WithStandards(CustomerStandard.VeryLow)
                      .AllowDirectApproach(true)
                      .GuaranteeFirstSample(false)
                      .WithMutualRelationRequirement(2f, 4f)
                      .WithCallPoliceChance(0f)
                      .WithDependence(1.6f, 2.4f)
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          (DrugType.Methamphetamine, 1.0f),
                          (DrugType.Cocaine, 0.85f),
                          (DrugType.Marijuana, 0.35f),
                          (DrugType.Shrooms, -0.20f)
                      });
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(2f)
                     .SetUnlocked(false)
                     .SetUnlockType(
                         NPCRelationship.UnlockType.Recommendation
                     )
                     .WithConnectionsById(new string[]
                     {
                         JessePinkman.NpcId
                     });
                })
                .WithSchedule(plan =>
                {

                    if (motel != null)
                        plan.StayInBuilding(
                            motel,
                            0,
                            390,
                            null
                        );

                    if (pillville != null)
                        plan.StayInBuilding(
                            pillville,
                            730,
                            90,
                            null,
                            "drugs"
                        );

                    if (pawnShop != null)
                        plan.StayInBuilding(
                            pawnShop,
                            1000,
                            75,
                            null,
                            "Pawn Shop"
                        );

                    if (chinese != null)
                        plan.StayInBuilding(
                            chinese,
                            1230,
                            90,
                            null,
                            "Lunch"
                        );

                    if (arcade != null)
                        plan.StayInBuilding(
                            arcade,
                            1445,
                            150,
                            null,
                            "Arcade"
                        );

                    NpcScheduleMixins.SitStop(
                        plan, 1700, 55, "Andrea - people watching");

                    plan.UseATM(
                        1800,
                        null,
                        "ATM"
                    );

                    if (nightclub != null)
                        plan.StayInBuilding(
                            nightclub,
                            1900,
                            240,
                            null,
                            "Night"
                        );

                    if (northApartments != null)
                        plan.StayInBuilding(
                            northApartments,
                            2330,
                            30,
                            null,
                            "Late"
                        );
                });
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            Appearance.Build();

            Aggressiveness = 0.08f;
            Region = Region.Westville;

            Schedule.Enable();

            Instance = this;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC] Andrea Cantillo loaded in Westville."
            );
        }
    }
}
