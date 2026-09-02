using CustomNPCExample.Products;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Map;
using S1API.Map.Buildings;
using System;
using System.Collections;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    /*
     * Gus Fenwick
     * Suburbia baking-supply supplier.
     * Sells the three ingredients needed to make brownies.
     * Payment drop: Behind Casino.
     * Unlock connection: Alison Knight.
     */
    public sealed class GusFenwick : NPC
    {
        public static GusFenwick Instance { get; private set; }

        public const string NpcId = "custom_gus_fenwick";
        public const string SupplierPersistentId = "wvc_gus_fenwick_v1";

        private const string PaymentDropName = "Baker's Casino Stash";

        private const string PaymentDropDescription =
            "Gus's payment spot behind the casino. " +
            "Leave the cash in the crate. He counts it twice.";

        private bool _dialogueSetupStarted;
        private bool _deadDropSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Gus] ConfigurePrefab started.");

            Building home =
                NPCBuildingLookup.Get("Upscale Apartments");

            Building northApartments =
                NPCBuildingLookup.Get("North apartments");

            Building supermarket =
                NPCBuildingLookup.Get("Supermarket");

            Building cafe =
                NPCBuildingLookup.Get("Cafe");

            Building chinese =
                NPCBuildingLookup.Get("Chinese Restaurant");

            Building pawnShop =
                NPCBuildingLookup.Get("Pawn Shop");

            Building budsBar =
                NPCBuildingLookup.Get("Bud's Bar");

            Building nightclub =
                NPCBuildingLookup.Get("Nightclub");

            Building shootingRange =
    NPCBuildingLookup.Get("Shooting Range");

            Building bradstent = NPCBuildingLookup.Get("Brad's Tent");
            Building pillville = NPCBuildingLookup.Get("Pillville");

            LogMissing(home, "Upscale Apartments");
            LogMissing(northApartments, "North apartments");
            LogMissing(supermarket, "Supermarket");
            LogMissing(cafe, "Cafe");
            LogMissing(chinese, "Chinese Restaurant");
            LogMissing(pawnShop, "Pawn Shop");
            LogMissing(budsBar, "Bud's Bar");
            LogMissing(nightclub, "Nightclub");

            Building fallback =
                home
                ?? northApartments
                ?? supermarket
                ?? cafe
                ?? chinese
                ?? pawnShop
                ?? budsBar
                ?? nightclub;

            Vector3 spawnPosition =
                new Vector3(
                    -22.0f,
                    -3.57f,
                    134.0f
                );

            builder
                .WithIdentity(
                    NpcId,
                    "Gus",
                    "Fenwick"
                )
                .WithRegion(
                    Region.Suburbia
                )
                .WithSpawnPosition(
                    spawnPosition,
                    Quaternion.Euler(
                        0f,
                        180f,
                        0f
                    )
                )
                .WithAppearanceDefaults(av =>
                {
                    /*
                     * Suburban baker / retired pastry chef.
                     */

                    av.Gender = 0f;
                    av.Height = 1.02f;
                    av.Weight = 0.92f;

                    av.SkinColor =
                        new Color32(
                            196,
                            148,
                            112,
                            255
                        );

                    av.LeftEyeLidColor =
                        av.SkinColor;

                    av.RightEyeLidColor =
                        av.SkinColor;

                    av.EyeBallTint =
                        new Color(
                            0.92f,
                            0.90f,
                            0.86f,
                            1f
                        );

                    av.PupilDilation = 0.48f;

                    av.EyebrowScale = 1.05f;
                    av.EyebrowThickness = 1.15f;
                    av.EyebrowRestingHeight = -0.05f;
                    av.EyebrowRestingAngle = -2f;

                    av.LeftEye =
                        new ValueTuple<float, float>(
                            0.49f,
                            0.50f
                        );

                    av.RightEye =
                        new ValueTuple<float, float>(
                            0.49f,
                            0.50f
                        );

                    av.HairPath =
                        "Avatar/Hair/Receding/Receding";

                    av.HairColor =
                        new Color32(
                            58,
                            44,
                            34,
                            255
                        );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SlightSmile",
                        new Color(
                            0.12f,
                            0.10f,
                            0.09f,
                            1f
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Goatee",
                        new Color32(
                            52,
                            40,
                            30,
                            255
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/OldPersonWrinkles",
                        new Color(
                            0.30f,
                            0.22f,
                            0.18f,
                            0.55f
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/ButtonUp",
                        new Color32(
                            238,
                            236,
                            230,
                            255
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color32(
                            44,
                            42,
                            40,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/ChefHat/ChefHat",
                        new Color32(
                            245,
                            245,
                            240,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Apron/Apron",
                        new Color32(
                            120,
                            70,
                            42,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/DressShoes/DressShoes",
                        new Color32(
                            34,
                            26,
                            20,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/SmallRoundGlasses/SmallRoundGlasses",
                        new Color32(
                            30,
                            30,
                            32,
                            255
                        )
                    );
                })
                .EnsureSupplier()
                .WithSupplierDefaults(s =>
                {
                    /*
                     * Three singular WithDeliveryItem(string) calls.
                     *
                     * This matches Remy's working pattern. String IDs are
                     * resolved lazily by S1API when the shop is opened,
                     * so it does not matter that BrownieIngredients has
                     * not registered yet at prefab time.
                     */
                    s.WithPersistentId(
                            SupplierPersistentId
                        )
                     .WithOrderLimits(
                            60f,
                            4000f
                        )
                     .WithStashDeadDrop<BehindCasino>()
                     .WithDeliveryItem(
                            BrownieIngredients.CocoaProductId
                        )
                     .WithDeliveryItem(
                            BrownieIngredients.ButterProductId
                        )
                     .WithDeliveryItem(
                            BrownieIngredients.LeavenProductId
                        )
                     .WithRecommendationMessage(
                            "Alison knows a quiet baker out in Suburbia. " +
                            "Name's Gus. He can get you brownie supplies."
                        )
                     .WithUnlockHint(
                            "Gus Fenwick unlocked. Order brownie ingredients. " +
                            "Pay behind the casino."
                        );
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(
                            1.4f
                        )
                     .SetUnlocked(
                            false
                        )
                     .SetUnlockType(
                            NPCRelationship.UnlockType.Recommendation
                        )
                     .WithConnectionsById(
                            new string[]
                            {
                                "alison_knight"
                            }
                        );
                })
                .WithSchedule(plan =>
                {
                    if (fallback == null)
                    {
                        MelonLogger.Warning(
                            "[Gus] No valid schedule buildings found."
                        );

                        return;
                    }

                    /*
                     * 24-hour Suburbia baker schedule.
                     *
                     * 00:00-06:00  Home
                     * 06:00-09:00  Supermarket
                     * 09:00-12:00  Cafe
                     * 12:00-14:00  Chinese Restaurant
                     * 14:00-16:00  Pawn Shop
                     * 16:00-19:00  Bud's Bar
                     * 19:00-23:00  Nightclub
                     * 23:00-00:00  Home
                     */

                    plan.StayInBuilding(
                        bradstent ?? fallback,
                        0,
                        360,
                        null,
                        "Home"
                    );

                    plan.StayInBuilding(
                        supermarket ?? fallback,
                        600,
                        180,
                        null,
                        "Morning Supplies"
                    );

                    plan.StayInBuilding(
                        pillville ?? fallback,
                        900,
                        180,
                        null,
                        "Cafe"
                    );

                    plan.StayInBuilding(
                        chinese ?? fallback,
                        1200,
                        120,
                        null,
                        "Lunch"
                    );

                    plan.StayInBuilding(
                        shootingRange ?? fallback,
                        1400,
                        120,
                        null,
                        "Errands"
                    );

                    plan.StayInBuilding(
                        budsBar ?? fallback,
                        1600,
                        180,
                        null,
                        "Bar"
                    );

                    plan.StayInBuilding(
                        nightclub ?? fallback,
                        1900,
                        240,
                        null,
                        "Nightclub"
                    );

                    plan.StayInBuilding(
                        home ?? northApartments ?? fallback,
                        2300,
                        60,
                        null,
                        "Home Late"
                    );

                    MelonLogger.Msg(
                        "[Gus] 24-hour Suburbia schedule assigned."
                    );
                });

            MelonLogger.Msg("[Gus] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.04f;
                Region = Region.Suburbia;

                Appearance.Build();

                try
                {
                    Schedule.Enable();

                    MelonLogger.Msg(
                        "[Gus] Schedule enabled."
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[Gus] Schedule.Enable failed: " +
                        ex.Message
                    );
                }

                if (!_dialogueSetupStarted)
                {
                    _dialogueSetupStarted = true;

                    MelonCoroutines.Start(
                        ConfigureDialogueAfterLoad()
                    );
                }

                if (!_deadDropSetupStarted)
                {
                    _deadDropSetupStarted = true;

                    MelonCoroutines.Start(
                        ConfigureExistingDeadDropAfterLoad()
                    );
                }

                MelonLogger.Msg(
                    "[Gus] Gus Fenwick loaded as Suburbia brownie-supply supplier."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Gus] OnCreated failed: " + ex
                );
            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (Dialogue != null &&
                    GusDialogue.TryRegisterAndArm(Dialogue))
                {
                    MelonLogger.Msg(
                        "[Gus] Baking supplier intro is ready."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            MelonLogger.Warning(
                "[Gus] Timed out waiting for dialogue."
            );
        }

        private IEnumerator ConfigureExistingDeadDropAfterLoad()
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                if (TryConfigureExistingDeadDrop())
                    yield break;

                yield return new WaitForSeconds(0.25f);
            }

            MelonLogger.Warning(
                "[Gus] Timed out waiting for Behind Casino drop."
            );
        }

        private static bool TryConfigureExistingDeadDrop()
        {
            try
            {
                DeadDropInstance selectedDrop =
                    DeadDropManager.Get<BehindCasino>();

                if (selectedDrop == null)
                    return false;

                string guid =
                    selectedDrop.GUID;

                if (string.IsNullOrWhiteSpace(guid))
                    return false;

                var nativeDrops =
                    DeadDrop.DeadDrops;

                if (nativeDrops == null ||
                    nativeDrops.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop =
                        nativeDrops[i];

                    if (drop == null)
                        continue;

                    bool match =
                        string.Equals(
                            drop.GUID.ToString(),
                            guid,
                            StringComparison.OrdinalIgnoreCase
                        ) ||
                        string.Equals(
                            drop.BakedGUID,
                            guid,
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (!match)
                        continue;

                    drop.DeadDropName =
                        PaymentDropName;

                    drop.DeadDropDescription =
                        PaymentDropDescription;

                    MelonLogger.Msg(
                        "[Gus] Behind Casino dead drop claimed: " +
                        guid
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Gus] Dead drop setup failed: " +
                    ex.Message
                );
            }

            return false;
        }

        private static void LogMissing(
            Building building,
            string name)
        {
            if (building != null)
                return;

            MelonLogger.Warning(
                "[Gus] Missing building: " + name
            );
        }
    }
}