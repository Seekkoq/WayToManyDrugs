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
    using S1API.Entities.Voices;
    using S1API.Map;
    using System;
    using System.Collections;
    using UnityEngine;
    using S1API.Entities.Voices;

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
                global::CustomNPCExample.Utils.WvcLog.Msg("[DamonTrey] ConfigurePrefab started.");

                Building homeBuilding = NPCBuildingLookup.Get("Bud's Bar");

                Vector3 spawnPosition =
                    new Vector3(-68f, -0.76f, -55f);

                builder
                    .WithIdentity(
                        NpcId,
                        "Damon",
                        "Trey"
                    )
                    .WithVoice(NPCVoiceCatalog.Redneck, 1.08f)
                    .WithSpawnPosition(
                        spawnPosition,
                        Quaternion.identity
                    )
                    .WithRegion(
                        Region.Docks
                    )
                    .WithAppearanceDefaults(av =>
                    {
                        av.Gender = 0f;
                        av.Height = 1.01f;
                        av.Weight = 0.88f;

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

                        av.EyeBallTint =
                            new Color(
                                0.58f,
                                0.50f,
                                0.34f
                            );

                        av.PupilDilation = 0.72f;

                        av.EyebrowScale = 1.24f;
                        av.EyebrowThickness = 1.42f;
                        av.EyebrowRestingHeight = -0.40f;
                        av.EyebrowRestingAngle = -9f;

                        av.HairPath =
                            "Avatar/Hair/JesusHair/JesusHair";

                        av.HairColor =
                            new Color32(
                                112,
                                78,
                                43,
                                255
                            );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Face_SmugPout",
                            new Color(
                                0.13f,
                                0.09f,
                                0.06f
                            )
                        );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/TiredEyes",
                            new Color32(
                                110,
                                72,
                                58,
                                255
                            )
                        );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/Freckles",
                            new Color32(
                                92,
                                58,
                                34,
                                210
                            )
                        );

                        av.WithFaceLayer(
                            "Avatar/Layers/Face/FacialHair_Stubble",
                            new Color32(
                                62,
                                42,
                                25,
                                255
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Top/FlannelButtonUp",
                            new Color32(
                                128,
                                52,
                                34,
                                255
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Bottom/Jorts",
                            new Color32(
                                66,
                                78,
                                92,
                                255
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Accessories/FingerlessGloves",
                            new Color32(
                                38,
                                31,
                                24,
                                255
                            )
                        );

                        av.WithBodyLayer(
                            "Avatar/Layers/Tattoos/LeftArm/LeftArm_Alien",
                            new Color32(
                                20,
                                75,
                                45,
                                255
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Head/CowboyHat/CowboyHat",
                            new Color32(
                                92,
                                58,
                                28,
                                255
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/FacialHair/Chevron/Chevron",
                            new Color32(
                                74,
                                48,
                                28,
                                255
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Neck/GoldChain/GoldChain",
                            new Color32(
                                190,
                                145,
                                55,
                                255
                            )
                        );

                        av.WithAccessoryLayer(
                            "Avatar/Accessories/Waist/Belt/Belt",
                            new Color32(
                                55,
                                32,
                                18,
                                255
                            )
                        );

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

                        .WithStashDeadDrop<
                            BehindLaundromat
                        >()

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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[DamonTrey] Schedule assigned: " +
                        "Bud's Bar, 00:00-24:00."
                    );
                }
                else
                {

                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
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
                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[DamonTrey] DMT supplier introduction is ready."
                        );

                        yield break;
                    }

                    yield return new WaitForSeconds(
                        0.25f
                    );
                }


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


            }

            private static bool TryConfigureExistingDeadDrop()
            {
                try
                {
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

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[DamonTrey] Dead drop claimed: " +
                            guid
                        );

                        return true;
                    }
                }
                catch (Exception)
                {

                }

                return false;
            }
        }
    }
