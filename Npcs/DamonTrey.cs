    using CustomNPCExample.Products;
    using Il2CppScheduleOne.Economy;
    using MelonLoader;
    using S1API.DeadDrops;
    using S1API.DeadDrops.Native;
    using S1API.Entities;
    using S1API.Entities.Appearances.AccessoryFields;
    using S1API.Entities.Appearances.BodyLayerFields;
    using S1API.Entities.Appearances.CustomizationFields;
    using S1API.Entities.Appearances.FaceLayerFields;
    using S1API.Entities.Relation;
    using S1API.Map;
    using System;
    using System.Collections;
    using UnityEngine;

    namespace CustomNPCExample.NPCs
    {
        public sealed class DamonTrey : NPC
        {
            public static DamonTrey Instance { get; private set; }

            public const string NpcId = "custom_damon_trey";
            public const string SupplierPersistentId = "wvc_damon_trey_v1";

            private const string PaymentDropName = "Damon's Payment Drop";

            private const string PaymentDropDescription =
                "Damon's payment drop. Leave what you owe here. " +
                "He counts it before you're off the block.";

            private bool _dialogueSetupStarted;
            private bool _deadDropSetupStarted;

            public override bool IsPhysical => true;
            public override bool IsSupplier => true;

            protected override void ConfigurePrefab(NPCPrefabBuilder builder)
            {
                MelonLogger.Msg("[DamonTrey] ConfigurePrefab started.");

                Building homeBuilding = NPCBuildingLookup.Get("Bud's Bar");

                // Offset from Roscoe so they don't stack.
                Vector3 spawnPosition =
                    new Vector3(-68f, -0.76f, -55f);

                builder
                    .WithIdentity(
                        NpcId,
                        "Damon",
                        "Trey"
                    )
                    .WithSpawnPosition(
                        spawnPosition,
                        Quaternion.identity
                    )
                    .WithRegion(
                        Region.Docks
                    )
                    .WithAppearanceDefaults(av =>
                    {
                        // Distinct hillbilly-stoner build
                        av.Gender = 0f;
                        av.Height = 1.01f;
                        av.Weight = 0.88f;

                        // Sun-baked / rough outdoor skin tone
                        av.SkinColor =
                            new Color32(
                                178,
                                126,
                                88,
                                255
                            );

                        av.LeftEyeLidColor =
                            av.SkinColor;

                        av.RightEyeLidColor =
                            av.SkinColor;

                        // Dull hazel/brown tired eyes
                        av.EyeBallTint =
                            new Color(
                                0.58f,
                                0.50f,
                                0.34f
                            );

                        // Stoner eyes, but not cartoonishly huge
                        av.PupilDilation = 0.72f;

                        // Heavy, droopy, uneven eyebrows
                        av.EyebrowScale = 1.24f;
                        av.EyebrowThickness = 1.42f;
                        av.EyebrowRestingHeight = -0.40f;
                        av.EyebrowRestingAngle = -9f;

                        // Long messy hillbilly hair
                        av.HairPath =
                            "Avatar/Hair/JesusHair/JesusHair";

                        av.HairColor =
                            new Color32(
                                112,
                                78,
                                43,
                                255
                            );

                        // Slightly smug / baked expression
                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Face_SmugPout",
                            new Color(
                                0.13f,
                                0.09f,
                                0.06f
                            )
                        );

                        // Tired under-eyes
                        av.WithFaceLayer(
                            "Avatar/Layers/Face/TiredEyes",
                            new Color32(
                                110,
                                72,
                                58,
                                255
                            )
                        );

                        // Rough complexion / freckles
                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Freckles",
                            new Color32(
                                92,
                                58,
                                34,
                                210
                            )
                        );

                        // Scruffy face
                        av.WithFaceLayer(
                            "Avatar/Layers/Face/FacialHair_Stubble",
                            new Color32(
                                62,
                                42,
                                25,
                                255
                            )
                        );

                        // Red-brown dirty flannel instead of Roscoe-style overalls
                        av.WithBodyLayer(
                            "Avatar/Layers/Top/FlannelButtonUp",
                            new Color32(
                                128,
                                52,
                                34,
                                255
                            )
                        );

                        // Cutoff denim jorts — makes him way less like Roscoe
                        av.WithBodyLayer(
                            "Avatar/Layers/Bottom/Jorts",
                            new Color32(
                                66,
                                78,
                                92,
                                255
                            )
                        );

                        // Fingerless gloves, dirty workwear vibe
                        av.WithBodyLayer(
                            "Avatar/Layers/Accessories/FingerlessGloves",
                            new Color32(
                                38,
                                31,
                                24,
                                255
                            )
                        );

                        // Optional weird stoner tattoo. Remove if it clips under clothing.
                        av.WithBodyLayer(
                            "Avatar/Layers/Tattoos/LeftArm/LeftArm_Alien",
                            new Color32(
                                20,
                                75,
                                45,
                                255
                            )
                        );

                        // Big visual difference from Roscoe: cowboy hat
                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Head/CowboyHat/CowboyHat",
                            new Color32(
                                92,
                                58,
                                28,
                                255
                            )
                        );

                        // Big hillbilly mustache
                        av.WithAccessoryLayer(
                            "Avatar/Accessories/FacialHair/Chevron/Chevron",
                            new Color32(
                                74,
                                48,
                                28,
                                255
                            )
                        );

                        // Slightly ridiculous chain, gives him character
                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Neck/GoldChain/GoldChain",
                            new Color32(
                                190,
                                145,
                                55,
                                255
                            )
                        );

                        // Worn leather belt
                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Waist/Belt/Belt",
                            new Color32(
                                55,
                                32,
                                18,
                                255
                            )
                        );

                        // Work boots instead of dress shoes
                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Feet/CombatBoots/CombatBoots",
                            new Color32(
                                38,
                                28,
                                19,
                                255
                            )
                        );
                    })
                    .WithSupplierDefaults(s => s
                        .WithPersistentId(
                            SupplierPersistentId
                        )
                        .WithOrderLimits(
                            100f,
                            4500f
                        )

                        // MUST be different from Roscoe's GreyDocksBuilding.
                        .WithStashDeadDrop<
                            BehindLaundromat
                        >()

                        // DMT extraction ingredients
                        .WithDeliveryItem(
                            DMTIngredients.DreamrootId
                        )
                        .WithDeliveryItem(
                            DMTIngredients.CausticBaseId
                        )
                        .WithDeliveryItem(
                            DMTIngredients.LabSolventId
                        )
                        .WithDeliveryItem(
                            DMTIngredients.CrystalizerId
                        )

                        .WithRecommendationMessage(
                            "I know a guy who handles specialty crystal stock. " +
                            "Name's Damon Trey. Quiet, careful, weird sense of humor. " +
                            "I passed him your number, he'll reach out."
                        )

                        .WithUnlockHint(
                            "Damon Trey is now available. You can order DMT supplies " +
                            "from him at Bud's Bar."
                        ))
                    .WithRelationshipDefaults(r => r
                        .WithDelta(
                            2.5f
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
                                "anna_chesterfield"
                            }
                        )
                    );

                if (homeBuilding != null)
                {
                    builder.WithSchedule(plan =>
                    {
                        plan.StayInBuilding(
                            homeBuilding,
                            0,
                            1440,
                            null,
                            null
                        );
                    });

                    MelonLogger.Msg(
                        "[DamonTrey] Schedule assigned: " +
                        "Bud's Bar, 00:00-24:00."
                    );
                }
                else
                {
                    MelonLogger.Warning(
                        "[DamonTrey] Bud's Bar was not found."
                    );
                }

                MelonLogger.Msg(
                    "[DamonTrey] ConfigurePrefab completed."
                );
            }

            protected override void OnCreated()
            {
                try
                {
                    base.OnCreated();

                    Instance = this;
                    Aggressiveness = 0f;
                    Region = Region.Docks;

                    Appearance.Build();
                    Schedule.Enable();

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
                        "[DamonTrey] Damon Trey loaded " +
                        "as a supplier at Bud's Bar."
                    );
                }
                catch (Exception ex)
                {
                    MelonLogger.Error(
                        "[DamonTrey] OnCreated failed: " +
                        ex
                    );
                }
            }

            private IEnumerator ConfigureDialogueAfterLoad()
            {
                for (int attempt = 0;
                     attempt < 60;
                     attempt++)
                {
                    if (Dialogue != null &&
                        DamonTreyDialogue.TryRegisterAndArm(
                            Dialogue
                        ))
                    {
                        MelonLogger.Msg(
                            "[DamonTrey] DMT supplier introduction is ready."
                        );

                        yield break;
                    }

                    yield return new WaitForSeconds(
                        0.25f
                    );
                }

                MelonLogger.Warning(
                    "[DamonTrey] Timed out waiting for dialogue components."
                );
            }

            private IEnumerator ConfigureExistingDeadDropAfterLoad()
            {
                for (int attempt = 0;
                     attempt < 80;
                     attempt++)
                {
                    if (TryConfigureExistingDeadDrop())
                        yield break;

                    yield return new WaitForSeconds(
                        0.25f
                    );
                }

                MelonLogger.Warning(
                    "[DamonTrey] Timed out waiting for dead drop."
                );
            }

            private static bool TryConfigureExistingDeadDrop()
            {
                try
                {
                    // Must match WithStashDeadDrop<T>() above.
                    // Must NOT be GreyDocksBuilding (Roscoe owns that).
                    DeadDropInstance selectedDrop =
                        DeadDropManager.Get<
                            BehindLaundromat
                        >();

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

                    for (int i = 0;
                         i < nativeDrops.Count;
                         i++)
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
                            "[DamonTrey] Dead drop claimed: " +
                            guid
                        );

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning(
                        "[DamonTrey] Dead drop setup failed: " +
                        ex.Message
                    );
                }

                return false;
            }
        }
    }