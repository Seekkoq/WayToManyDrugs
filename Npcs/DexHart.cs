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
using System;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class DexHart : NPC
    {
        public static DexHart Instance;
        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Building home = NPCBuildingLookup.Get("North apartments");
            Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
            Building pawnShop = NPCBuildingLookup.Get("Pawn Shop");
            Building hardware = NPCBuildingLookup.Get("Dan's Hardware Upstairs");
            Building shootingRange = NPCBuildingLookup.Get("Shooting Range");
            Building nightclub = NPCBuildingLookup.Get("Nightclub");
            Building motel = NPCBuildingLookup.Get("Motel Office");

            Vector3 spawnPos = new Vector3(-68.5f, -0.76f, -66f);

            builder
                .WithIdentity("custom_dex_hart", "Dex", "Hart")
                // VOICE REMOVED
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f; av.Height = 1f; av.Weight = 0.82f;
                    av.SkinColor = new Color(0.72f, 0.59f, 0.51f);
                    av.LeftEyeLidColor = av.SkinColor; av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.65f, 0.72f, 0.76f); av.PupilDilation = 0.72f;
                    av.EyebrowScale = 1.10f; av.EyebrowThickness = 1.22f;
                    av.HairColor = new Color(0.05f, 0.05f, 0.05f);
                    av.HairPath = "Avatar/Hair/Spiky/Spiky";
                    av.WithFaceLayer("Avatar/Layers/Face/Face_Neutral", new Color(0.08f, 0.08f, 0.08f));
                    av.WithBodyLayer("Avatar/Layers/Bottom/Jeans", new Color(0.07f, 0.08f, 0.10f));
                    av.WithBodyLayer("Avatar/Layers/Top/T-Shirt", new Color(0.08f, 0.22f, 0.15f));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/Sneakers/Sneakers", new Color(0.18f, 0.12f, 0.08f));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color(0.14f, 0.15f, 0.16f));
                })
                .WithSpawnPosition(spawnPos)
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(1000f, 2200f).WithOrdersPerWeek(2, 4).WithPreferredOrderDay(Day.Sunday).WithOrderTime(2200)
                      .WithStandards(CustomerStandard.VeryLow).AllowDirectApproach(true).GuaranteeFirstSample(false)
                      .WithMutualRelationRequirement(5f, 7f).WithCallPoliceChance(0f).WithDependence(0.35f, 1.25f)
                      .WithAffinities(new ValueTuple<DrugType, float>[]
                      {
                          (DrugType.Marijuana, 0.20f),
                          (DrugType.Methamphetamine, 0.94f),
                          (DrugType.Cocaine, 0.66f),
                          (DrugType.Shrooms, -0.72f)
                      })
                      .WithPreferredProperties(new PropertyBase[] { Property.Energizing, Property.AntiGravity, Property.Sneaky });
                })
.WithRelationshipDefaults(r =>
{
    r.WithDelta(1f)
     .SetUnlocked(false)
     .SetUnlockType(
         NPCRelationship.UnlockType.DirectApproach
     )
     .WithConnectionsById(new string[]
     {
         "lucy_pennington"
     });
})
                .WithSchedule(plan =>
                {
                    plan.StayInBuilding(home, 600, 120, null, null);
                    plan.StayInBuilding(gasMart, 830, 45, null, null);
                    plan.StayInBuilding(pawnShop, 1000, 120, null, null);
                    plan.StayInBuilding(hardware, 1230, 90, null, null);
                    plan.StayInBuilding(shootingRange, 1430, 180, null, null);
                    plan.UseATM(1800, null, null);
                    plan.StayInBuilding(nightclub, 1900, 270, null, null);
                    plan.StayInBuilding(motel, 0, 360, null, null);
                });
        }


        protected override void OnCreated()
        {
            base.OnCreated(); Appearance.Build(); Aggressiveness = 0f;
            Region = Region.Downtown; Schedule.Enable(); Instance = this;
            MelonLogger.Msg("Dex Hart loaded in Downtown!");
        }
    }
}