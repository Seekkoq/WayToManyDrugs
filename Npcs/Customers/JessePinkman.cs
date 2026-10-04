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
    using S1API.Entities.Voices;
    using System;
    using UnityEngine;

    namespace CustomNPCExample.NPCs
    {
        public sealed class JessePinkman : NPC
        {
            public static JessePinkman Instance;

            public const string NpcId = "custom_jesse_pinkman";

            public override bool IsPhysical => true;

            protected override void ConfigurePrefab(NPCPrefabBuilder builder)
            {
                Building motel = NPCBuildingLookup.Get("Motel Office");
                Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
                Building pawnShop = NPCBuildingLookup.Get("Pawn Shop");
                Building chinese = NPCBuildingLookup.Get("Chinese Restaurant");
                Building arcade = NPCBuildingLookup.Get("Arcade");
                Building nightclub = NPCBuildingLookup.Get("Nightclub");
            Building shootingRange =
    NPCBuildingLookup.Get("Shooting Range");
            Building bradstent = NPCBuildingLookup.Get("Brad's Tent");
            Building pillville = NPCBuildingLookup.Get("Pillville");

            if (motel == null) { }
                if (gasMart == null) { }
                if (pawnShop == null) { }
                if (chinese == null) { }
                if (arcade == null) { }
                if (nightclub == null) { }

                Vector3 spawnPos = new Vector3(-54.2f, -0.76f, -41.8f);

                builder
                    .WithIdentity(NpcId, "Jesse", "Pinkman")
                    .WithVoice(NPCVoiceCatalog.Tyler, 1.0f)
                    .WithRegion(Region.Westville)
                    .WithAppearanceDefaults(av =>
                    {
                        av.Gender = 0f;
                        av.Height = 0.96f;
                        av.Weight = 0.62f;

                        av.SkinColor = new Color32(214, 176, 148, 255);
                        av.LeftEyeLidColor = av.SkinColor;
                        av.RightEyeLidColor = av.SkinColor;

                        av.EyeBallTint = new Color(0.78f, 0.82f, 0.84f);
                        av.PupilDilation = 0.58f;

                        av.EyebrowScale = 0.92f;
                        av.EyebrowThickness = 0.88f;
                        av.EyebrowRestingHeight = 0.06f;
                        av.EyebrowRestingAngle = -4f;

                        av.HairPath = "Avatar/Hair/Spiky/Spiky";
                        av.HairColor = new Color32(148, 112, 72, 255);

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Face_Neutral",
                            new Color(0.12f, 0.10f, 0.09f)
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Top/T-Shirt",
                            new Color32(206, 188, 164, 255)
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Bottom/Jeans",
                            new Color32(48, 62, 82, 255)
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                            new Color32(28, 26, 24, 255)
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Feet/Sneakers/Sneakers",
                            new Color32(210, 208, 202, 255)
                        );
                    })
                    .WithSpawnPosition(spawnPos)
                    .EnsureCustomer()
                    .WithCustomerDefaults(cd =>
                    {
                        cd.WithSpending(350f, 1100f)
                          .WithOrdersPerWeek(3, 5)
                          .WithPreferredOrderDay(Day.Saturday)
                          .WithOrderTime(2130)
                          .WithStandards(CustomerStandard.VeryLow)
                          .AllowDirectApproach(true)
                          .GuaranteeFirstSample(true)
                          .WithMutualRelationRequirement(3f, 5f)
                          .WithCallPoliceChance(0.02f)
                          .WithDependence(1f, 1.40f)
                          .WithAffinities(new ValueTuple<DrugType, float>[]
                          {
                              (DrugType.Methamphetamine, 2f),
                              (DrugType.Marijuana, 0.52f),
                              (DrugType.Cocaine, 0.38f),
                              (DrugType.Shrooms, -0.45f)
                          })
                          .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Euphoric,
                                Property.Foggy
                            }
                        );
                    })
                    .WithRelationshipDefaults(r =>
                    {
                        r.WithDelta(0.8f)
                         .SetUnlocked(false)
                         .SetUnlockType(NPCRelationship.UnlockType.Recommendation)
                         .WithConnectionsById(new string[] { "jerry_montero" });
                    })
                    .WithSchedule(plan =>
                    {
                        if (motel != null)
                            plan.StayInBuilding(motel, 200, 520, null, "Sleep");

                        if (gasMart != null)
                            plan.StayInBuilding(gasMart, 945, 50, null, "Breakfast junk");

                        if (pawnShop != null)
                            plan.StayInBuilding(bradstent, 1130, 80, null, "Pawn shop");

                        if (chinese != null)
                            plan.StayInBuilding(shootingRange, 1330, 70, null, "Chinese");

                        NpcScheduleMixins.VendingStop(
                            plan, 1445, "Jesse - vending machine");

                        if (arcade != null)
                            plan.StayInBuilding(arcade, 1530, 110, null, "Arcade");

                        plan.UseATM(1830, null, "ATM");

                        if (nightclub != null)
                            plan.StayInBuilding(nightclub, 2030, 210, null, "Night");
                    });
            }

            protected override void OnCreated()
            {
                base.OnCreated();

                Appearance.Build();
                Aggressiveness = 1f;
                Region = Region.Westville;
                Schedule.Enable();
                Instance = this;

                global::CustomNPCExample.Utils.WvcLog.Msg("[Jesse] Jesse Pinkman loaded in Westville.");
            }
        }
    }
