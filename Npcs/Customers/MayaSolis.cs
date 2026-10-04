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
    public sealed class MayaSolis : NPC
    {
        public static MayaSolis Instance;
        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Building apartment = NPCBuildingLookup.Get("North apartments");
            Building cafe = NPCBuildingLookup.Get("Cafe");
            Building supermarket = NPCBuildingLookup.Get("Supermarket");
            Building arcade = NPCBuildingLookup.Get("Arcade");
            Building bar = NPCBuildingLookup.Get("Bud's Bar");
            Building home = NPCBuildingLookup.Get("Upscale Apartments");

            Vector3 spawnPos = new Vector3(-63.5f, -0.76f, -71f);

            builder
                .WithIdentity("custom_maya_solis", "Maya", "Solis")
                .WithVoice(NPCVoiceCatalog.Female1, 1.02f)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 1f; av.Height = 0.96f; av.Weight = 0.42f;
                    av.SkinColor = new Color(0.42f, 0.25f, 0.17f);
                    av.LeftEyeLidColor = av.SkinColor; av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.72f, 0.82f, 0.76f); av.PupilDilation = 0.58f;
                    av.EyebrowScale = 0.90f; av.EyebrowThickness = 0.80f;
                    av.HairColor = new Color(0.12f, 0.035f, 0.025f);
                    av.HairPath = "Avatar/Hair/Spiky/Spiky";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_Neutral", new Color(0.18f, 0.08f, 0.06f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.10f, 0.17f, 0.29f));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.48f, 0.06f, 0.12f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(0.08f, 0.08f, 0.10f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color(0.62f, 0.49f, 0.33f));
                })
                .WithSpawnPosition(spawnPos)
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(700f, 1400f).WithOrdersPerWeek(3, 5).WithPreferredOrderDay(Day.Wednesday).WithOrderTime(1500)
                      .WithStandards(CustomerStandard.VeryLow).AllowDirectApproach(true).GuaranteeFirstSample(false)
                      .WithMutualRelationRequirement(3f, 5f).WithCallPoliceChance(0f).WithDependence(0.12f, 0.80f)
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          (DrugType.Marijuana, 0.62f),
                          (DrugType.Shrooms, 0.95f),
                          (DrugType.Cocaine, -0.35f),
                          (DrugType.Methamphetamine, -0.82f)
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
    r.WithDelta(1.5f)
     .SetUnlocked(false)
     .SetUnlockType(
         NPCRelationship.UnlockType.Recommendation
     )
     .WithConnectionsById(new string[]
     {
         "donna_martin"
     });
})
                .WithSchedule(plan =>
                {
                    plan.StayInBuilding(apartment, 0, 565, null, null);
                    plan.StayInBuilding(cafe, 925, 120, null, null);
                    plan.UseVendingMachine(1030, null, "Maya - vending machine");
                    plan.StayInBuilding(supermarket, 1045, 90, null, null);
                    plan.StayInBuilding(arcade, 1245, 130, null, null);
                    NpcScheduleMixins.SitStop(plan, 1400, 50, "Maya - park bench");
                    plan.UseATM(1500, null, "Maya - shopping cash");
                    plan.StayInBuilding(bar, 1600, 150, null, null);
                    plan.StayInBuilding(home, 1700, 740, null, null);
                });
        }

        protected override void OnCreated()
        {
            base.OnCreated(); Appearance.Build(); Aggressiveness = 0f;
            Region = Region.Northtown; Schedule.Enable(); Instance = this;
            global::CustomNPCExample.Utils.WvcLog.Msg("Maya Solis loaded in Northtown!");
        }
    }
}
