using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.Entities.Schedule;
using S1API.GameTime;
using S1API.Map;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class SlideMorales : NPC
    {
        public static SlideMorales Instance;

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Building randysBaitAndTackle = NPCBuildingLookup.Get("Randy's Bait & Tackle");
            Building docksContainer = NPCBuildingLookup.Get("Docks Shipping Container");
            Building fishWarehouse = NPCBuildingLookup.Get("Fish Warehouse");
            Building budsBar = NPCBuildingLookup.Get("Bud's Bar");

            Vector3 spawnPos = new Vector3(-68.5f, -0.76f, -66f);

            builder
                .WithIdentity(
                    "custom_vincent_morales_vip",
                    "Vincent",
                    "Morales"
                )

                .WithAppearanceDefaults(av =>
                {
                    // Gangster / Docks regular look
                    av.Gender = 0f;
                    av.Height = 1.0f;
                    av.Weight = 0.62f;

                    av.SkinColor = new Color(0.62f, 0.47f, 0.36f);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(0.85f, 0.85f, 0.82f);
                    av.PupilDilation = 0.6f;

                    av.EyebrowScale = 1.05f;
                    av.EyebrowThickness = 1.3f;
                    av.EyebrowRestingHeight = -0.30f;
                    av.EyebrowRestingAngle = -6f;

                    av.LeftEye = new ValueTuple<float, float>(0.47f, 0.5f);
                    av.RightEye = new ValueTuple<float, float>(0.47f, 0.5f);

                    av.HairColor = new Color(0.05f, 0.04f, 0.04f);
                    av.HairPath = "Avatar/Hair/Spiky/Spiky";

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        Color.black
                    );

                    // Black jeans
                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(0.08f, 0.08f, 0.09f)
                    );

                    // Dark red shirt
                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color(0.45f, 0.06f, 0.08f)
                    );

                    // Safe working accessories
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color(0.10f, 0.08f, 0.06f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/CollarJacket/CollarJacket",
                        new Color(0.10f, 0.09f, 0.10f)
                    );
                })

                .WithSpawnPosition(spawnPos)

                .EnsureCustomer()

                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(500f, 800f)

                      .WithOrdersPerWeek(3, 5)

                      .WithPreferredOrderDay(Day.Saturday)
                      .WithOrderTime(1900)

                      // CHANGED: was VeryLow
                      .WithStandards(CustomerStandard.Moderate)

                      .AllowDirectApproach(true)
                      .GuaranteeFirstSample(false)

                      .WithMutualRelationRequirement(3f, 5f)

                      .WithCallPoliceChance(0f)
                      .WithDependence(0.18f, 0.95f)

                      // More stoner / chill VIP than before
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          new ValueTuple<DrugType, float>(DrugType.Marijuana, 0.90f),
                          new ValueTuple<DrugType, float>(DrugType.Shrooms, 0.70f),
                          new ValueTuple<DrugType, float>(DrugType.Cocaine, -0.20f),
                          new ValueTuple<DrugType, float>(DrugType.Methamphetamine, -0.60f)
                      })

                      // CHANGED: different favorite effects/properties
                      .WithPreferredProperties(new PropertyBase[]
                      {
                          Property.Toxic,
                          Property.Spicy,
                          Property.Paranoia,
                      });
                })

                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(1.5f)
                     .SetUnlocked(false)
                     .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)

                     // CHANGED: unlock through Lisa Gardener instead of Jax
                     .WithConnectionsById(new string[]
                     {
                         "lisa_gardener"
                     });
                })

                .WithSchedule(plan =>
                {
                    // 00:00–06:00: Shipping container
                    plan.StayInBuilding(docksContainer, 0, 360, null, null);

                    // 06:00–08:00: Fish Warehouse
                    plan.StayInBuilding(fishWarehouse, 600, 120, null, null);

                    // 08:00–17:00: Main hangout
                    plan.StayInBuilding(randysBaitAndTackle, 800, 540, null, null);

                    // 17:00–18:30: Bud's Bar
                    plan.StayInBuilding(budsBar, 1700, 90, null, null);

                    // 18:30–00:00: Back to Randy's
                    plan.StayInBuilding(randysBaitAndTackle, 1830, 330, null, null);
                });
        }

        protected override void OnCreated()
        {
            MelonLogger.Msg("Loading VIP customer: Vincent Morales");

            try
            {
                base.OnCreated();

                Appearance.Build();

                Aggressiveness = 1f;
                Region = Region.Docks;

                Schedule.Enable();

                Instance = this;

                MelonLogger.Msg(
                    "Vincent Morales loaded as VIP Docks customer. " +
                    "Main location: Randy's Bait & Tackle."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "Vincent Morales OnCreated failed: " + ex.Message
                );

                MelonLogger.Error(
                    "StackTrace: " + ex.StackTrace
                );
            }
        }
    }
}