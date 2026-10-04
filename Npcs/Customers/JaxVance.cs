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
    public sealed class Example : NPC
    {
        public static Example Instance;
        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Building home = NPCBuildingLookup.Get("North apartments");
            Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
            Building arcade = NPCBuildingLookup.Get("Arcade");
            Building chinese = NPCBuildingLookup.Get("Chinese Restaurant");
            Building cafe = NPCBuildingLookup.Get("Cafe");
            Building bar = NPCBuildingLookup.Get("Bud's Bar");
            Building nightclub = NPCBuildingLookup.Get("Nightclub");

            Vector3 spawnPos = new Vector3(-73.5f, -0.76f, -61f);

            builder
                .WithIdentity("custom_jax_vance", "Jax", "Vance")
                .WithVoice(NPCVoiceCatalog.Cold, 1.0f)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f; av.Height = 1.05f; av.Weight = 0.45f;
                    av.SkinColor = new Color(0.82f, 0.71f, 0.62f);
                    av.LeftEyeLidColor = av.SkinColor; av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.95f, 0.95f, 0.95f); av.PupilDilation = 0.85f;
                    av.EyebrowScale = 1.0f; av.EyebrowThickness = 1.2f;
                    av.HairColor = new Color(0.10f, 0.10f, 0.12f);
                    av.HairPath = "Avatar/Hair/Spiky/Spiky";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_Neutral", Color.black);
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.15f, 0.15f, 0.20f));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.25f, 0.10f, 0.35f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(0.90f, 0.90f, 0.90f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color(0.12f, 0.12f, 0.14f));
                })
                .WithSpawnPosition(spawnPos)
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(600f, 1500f).WithOrdersPerWeek(5, 7).WithPreferredOrderDay(Day.Friday).WithOrderTime(1600)
                      .WithStandards(CustomerStandard.VeryLow).AllowDirectApproach(true).GuaranteeFirstSample(true)
                      .WithMutualRelationRequirement(1f, 3f).WithCallPoliceChance(0f).WithDependence(0.3f, 1.2f)
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          (DrugType.Marijuana, 0.90f),
                          (DrugType.Shrooms, 0.75f),
                          (DrugType.Cocaine, 0.30f),
                          (DrugType.Methamphetamine, -0.60f)
                      })
                      .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Energizing,
                                Property.Paranoia
                            }
                        );
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
         "doris_lubbin"
     });
                })
                .WithSchedule(plan =>
                {
                    plan.StayInBuilding(home, 0, 385, null, null);
                    plan.StayInBuilding(home, 625, 80, null, null);
                    plan.StayInBuilding(gasMart, 745, 80, null, null);
                    plan.StayInBuilding(arcade, 845, 210, null, null);
                    plan.UseATM(1100, null, "Jax - arcade cash");
                    NpcScheduleMixins.SitStop(plan, 1130, 45, "Jax - bench break");
                    plan.StayInBuilding(chinese, 1245, 70, null, null);
                    plan.StayInBuilding(cafe, 1355, 75, null, null);
                    plan.StayInBuilding(bar, 1450, 105, null, null);
                    plan.StayInBuilding(nightclub, 1655, 745, null, null);
                });
        }

        protected override void OnCreated()
        {
            base.OnCreated(); Appearance.Build(); Aggressiveness = 0f;
            Region = Region.Westville; Schedule.Enable(); Instance = this;
            global::CustomNPCExample.Utils.WvcLog.Msg("Jax Vance loaded in Westville!");
        }
    }
}
