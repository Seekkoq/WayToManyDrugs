using System;
using MelonLoader;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using S1API.Map;
using UnityEngine;
using S1API.Entities.Voices;

using S1DealerType = S1API.Economy.DealerType;
using S1Region = S1API.Map.Region;

namespace CustomNPCExample.NPCs
{
    public sealed class NickDime : NPC
    {
        public static NickDime Instance { get; private set; }

        public const string NpcId = "custom_nick_dime";
        public const string RecommenderNpcId = "jessi_waters";

        public const float SigningFee = 1000f;
        public const float Commission = 0.10f;

        private const string HomeBuildingName = "North apartments";

        public static Vector3? SpawnPositionOverride { get; set; }
        public static float SpawnYaw { get; set; } = 135f;

        private NPCDealer _dealerEvents;

        public override bool IsPhysical => true;
        public override bool IsDealer => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[Nick Dime] ConfigurePrefab started.");

            Building home =
                NPCBuildingLookup.Get(HomeBuildingName);

            Building cafe =
                NPCBuildingLookup.Get("Cafe");

            Building pawnShop =
                NPCBuildingLookup.Get("Pawn Shop");

            Building supermarket =
                NPCBuildingLookup.Get("Supermarket");

            Building nightclub =
                NPCBuildingLookup.Get("Nightclub");

            LogMissingBuilding(home, HomeBuildingName);
            LogMissingBuilding(cafe, "Cafe");
            LogMissingBuilding(pawnShop, "Pawn Shop");
            LogMissingBuilding(supermarket, "Supermarket");
            LogMissingBuilding(nightclub, "Nightclub");

            Building fallback =
                home ?? cafe ?? pawnShop ?? supermarket ?? nightclub;

            if (SpawnPositionOverride.HasValue)
            {
                builder.WithSpawnPosition(
                    SpawnPositionOverride.Value,
                    Quaternion.Euler(0f, SpawnYaw, 0f)
                );
            }

            builder
                .WithIdentity(NpcId, "Nick", "Dime")
                .WithVoice(NPCVoiceCatalog.Cold, 0.97f)
                .WithRegion(S1Region.Northtown)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 1.02f;
                    av.Weight = 0.43f;

                    av.SkinColor = new Color32(190, 150, 119, 255);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint = new Color(0.94f, 0.95f, 0.90f);
                    av.PupilDilation = 0.45f;

                    av.EyebrowScale = 1.02f;
                    av.EyebrowThickness = 0.90f;
                    av.EyebrowRestingHeight = 0.02f;
                    av.EyebrowRestingAngle = -2f;

                    av.LeftEye =
                        new ValueTuple<float, float>(0.50f, 0.48f);

                    av.RightEye =
                        new ValueTuple<float, float>(0.50f, 0.48f);

                    av.HairPath =
                        "Avatar/Hair/LongSlicked/LongSlicked";

                    av.HairColor = new Color32(36, 28, 23, 255);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SmugPout",
                        new Color(0.14f, 0.09f, 0.06f, 1f)
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Stubble",
                        new Color32(50, 38, 30, 165)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/ButtonUp",
                        new Color32(224, 218, 193, 255)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color32(35, 39, 46, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/Blazer/Blazer",
                        new Color32(35, 77, 78, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/RectangleFrameGlasses/RectangleFrameGlasses",
                        new Color32(46, 43, 36, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Hands/Polex/Polex",
                        new Color32(181, 145, 65, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/DressShoes/DressShoes",
                        new Color32(54, 38, 27, 255)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color32(48, 33, 24, 255)
                    );
                })
                .WithDealerDefaults(dealer =>
                {
                    dealer
                        .WithDealerType(S1DealerType.PlayerDealer)
                        .WithSigningFee(SigningFee)
                        .WithCut(Commission)
                        .WithHomeName(HomeBuildingName)
                        .WithRecommendation(recommendation =>
                        {
                            recommendation
                                .FromCustomer(RecommenderNpcId)
                                .OnDealCompleted();
                        });

                    if (home != null)
                        dealer.WithHome(home);
                })
                .WithRelationshipDefaults(relationship =>
                {
                    relationship
                        .WithDelta(1.0f)
                        .SetUnlocked(false)
                        .SetUnlockType(
                            NPCRelationship.UnlockType.Recommendation
                        )
                        .WithConnectionsById(
                            new[] { RecommenderNpcId }
                        );
                })
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {

                        return;
                    }

                    plan.StayInBuilding(
                        home ?? fallback,
                        0, 435, null,
                        "Nick - Close the books"
                    );

                    plan.StayInBuilding(
                        cafe ?? fallback,
                        715, 85, null,
                        "Nick - Coffee and receipts"
                    );

                    plan.StayInBuilding(
                        pawnShop ?? fallback,
                        840, 210, null,
                        "Nick - Appraisals and accounts"
                    );

                    plan.StayInBuilding(
                        supermarket ?? fallback,
                        1230, 60, null,
                        "Nick - Compare the unit prices"
                    );

                    plan.StayInBuilding(
                        cafe ?? fallback,
                        1330, 210, null,
                        "Nick - Afternoon ledger"
                    );

                    plan.StayInBuilding(
                        nightclub ?? fallback,
                        1700, 180, null,
                        "Nick - Evening contacts"
                    );

                    plan.StayInBuilding(
                        cafe ?? fallback,
                        2000, 120, null,
                        "Nick - Final appointments"
                    );

                    plan.StayInBuilding(
                        home ?? fallback,
                        2200, 120, null,
                        "Nick - Count it twice"
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Nick Dime] Complete daily schedule configured."
                    );
                });

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Nick Dime] Prefab configured. Fee=$1000, cut=10%."
            );
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;

                Appearance.Build();
                Aggressiveness = 0.04f;
                Region = S1Region.Northtown;

                _dealerEvents = Dealer;

                if (_dealerEvents != null && _dealerEvents.IsDealer)
                {
                    Building home =
                        NPCBuildingLookup.Get(HomeBuildingName);

                    if (home != null)
                        _dealerEvents.Home = home;

                    _dealerEvents.OnRecruited -= HandleRecruited;
                    _dealerEvents.OnRecruited += HandleRecruited;
                }
                else
                {

                }

                if (Schedule != null)
                    Schedule.Enable();

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Nick Dime] Loaded as a Northtown player dealer."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Nick Dime] OnCreated failed: " + ex
                );
            }
        }

        private void HandleRecruited()
        {
            try
            {
                SendTextMessage(
                    "Nick Dime. One dime on every dollar; the other ninety " +
                    "cents are yours. Bring me packaged stock and assign " +
                    "your customers. I'll handle the route and the receipts. " +
                    "No surprises. That's the whole pitch."
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Nick Dime] Recruitment event received."
                );
            }
            catch (Exception)
            {

            }
        }

        protected override void OnDestroyed()
        {
            try
            {
                if (_dealerEvents != null)
                    _dealerEvents.OnRecruited -= HandleRecruited;
            }
            catch (Exception)
            {

            }
            finally
            {
                _dealerEvents = null;

                if (ReferenceEquals(Instance, this))
                    Instance = null;

                base.OnDestroyed();
            }
        }

        private static void LogMissingBuilding(
            Building building,
            string name)
        {
            if (building == null)
            {

            }
        }
    }
}
