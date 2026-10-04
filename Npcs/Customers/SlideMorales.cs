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
using System;
using UnityEngine;
using S1API.Entities.Voices;

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
                .WithIdentity("custom_vincent_morales_vip", "Vincent", "Morales")
                .WithVoice(NPCVoiceCatalog.Monotone, 0.94f)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f; av.Height = 1f; av.Weight = 0.62f;
                    av.SkinColor = new Color(0.62f, 0.47f, 0.36f);
                    av.LeftEyeLidColor = av.SkinColor; av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.85f, 0.85f, 0.82f); av.PupilDilation = 0.6f;
                    av.EyebrowScale = 1.05f; av.EyebrowThickness = 1.30f;
                    av.EyebrowRestingHeight = -0.3f; av.EyebrowRestingAngle = -6f;
                    av.LeftEye = (0.47f, 0.5f); av.RightEye = (0.47f, 0.5f);
                    av.HairColor = new Color(0.05f, 0.04f, 0.04f);
                    av.HairPath = "Avatar/Hair/Spiky/Spiky";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_Neutral", Color.black);
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.08f, 0.08f, 0.09f));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.45f, 0.06f, 0.08f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(0.10f, 0.08f, 0.06f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color(0.10f, 0.09f, 0.10f));
                })
                .WithSpawnPosition(spawnPos)
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(500f, 800f).WithOrdersPerWeek(3, 5).WithPreferredOrderDay(Day.Saturday).WithOrderTime(1900)
                      .WithStandards(CustomerStandard.Moderate).AllowDirectApproach(true).GuaranteeFirstSample(false)
                      .WithMutualRelationRequirement(3f, 5f).WithCallPoliceChance(0f).WithDependence(0.18f, 0.95f)
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          (DrugType.Marijuana, 0.90f),
                          (DrugType.Shrooms, 0.70f),
                          (DrugType.Cocaine, -0.20f),
                          (DrugType.Methamphetamine, -0.60f)
                      })
                      .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Munchies,
                                Property.Calming
                            }
                        );
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(1.5f)
                     .SetUnlocked(false)
                     .SetUnlockType(NPCRelationship.UnlockType.DirectApproach)
                     .WithConnectionsById(new string[] { "lisa_gardener" });
                })
                .WithSchedule(plan =>
                {
                    plan.StayInBuilding(docksContainer, 0, 380, null, null);
                    plan.StayInBuilding(fishWarehouse, 620, 100, null, null);
                    plan.StayInBuilding(randysBaitAndTackle, 800, 240, null, null);
                    NpcScheduleMixins.SitStop(plan, 1230, 50, "Slide - docks bench");
                    plan.StayInBuilding(randysBaitAndTackle, 1330, 210, null, null);
                    plan.StayInBuilding(budsBar, 1700, 90, null, null);
                    plan.StayInBuilding(randysBaitAndTackle, 1900, 300, null, null);
                });
        }

        protected override void OnCreated()
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("Loading VIP customer: Vincent Morales");

            try
            {
                base.OnCreated();
                Appearance.Build();
                Aggressiveness = 1f;
                Region = Region.Docks;
                Schedule.Enable();
                Instance = this;
                global::CustomNPCExample.Utils.WvcLog.Msg("Vincent Morales loaded as VIP Docks customer. Main location: Randy's Bait & Tackle.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Vincent Morales OnCreated failed: " + ex.Message);
                MelonLogger.Error("StackTrace: " + ex.StackTrace);
            }
        }
    }
}