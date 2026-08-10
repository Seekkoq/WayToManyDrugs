using CustomNPCExample.Products;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using S1API.DeadDrops;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Map;
using System;
using System.Collections;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class RoscoeBellweather : NPC
    {
        public static RoscoeBellweather Instance
        {
            get;
            private set;
        }

        public const string NpcId =
            "custom_roscoe_bellweather";

        public const string SupplierPersistentId =
            "wvc_roscoe_bellweather";

        private const string PaymentDropName =
            "Roscoe's Payment Drop";

        private const string PaymentDropDescription =
            "Roscoe's payment drop. Leave what you owe here. " +
            "He'll know when the cash lands.";

        private bool _deadDropSetupStarted;
        private bool _dialogueSetupStarted;

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(
            NPCPrefabBuilder builder)
        {
            MelonLogger.Msg(
                "[Roscoe] ConfigurePrefab started."
            );

            Building budsBar =
                NPCBuildingLookup.Get("Bud's Bar");

            Vector3 spawnPosition =
                new Vector3(-72f, -0.76f, -58f);

            builder
                .WithIdentity(
                    NpcId,
                    "Roscoe",
                    "Bellweather"
                )
                .WithSpawnPosition(
                    spawnPosition,
                    Quaternion.identity
                )
                .WithRegion(Region.Docks)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.94f;
                    av.Weight = 0.88f;

                    av.SkinColor =
                        new Color(0.78f, 0.60f, 0.49f);

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint =
                        new Color(0.82f, 0.84f, 0.80f);

                    av.PupilDilation = 0.38f;

                    av.EyebrowScale = 1.24f;
                    av.EyebrowThickness = 1.40f;
                    av.EyebrowRestingHeight = -0.34f;
                    av.EyebrowRestingAngle = -9f;

                    av.LeftEye =
                        new ValueTuple<float, float>(
                            0.34f,
                            0.46f
                        );

                    av.RightEye =
                        new ValueTuple<float, float>(
                            0.34f,
                            0.46f
                        );

                    // The beanie is the only head covering.
                    av.HairPath = string.Empty;

                    av.HairColor =
                        new Color(0.62f, 0.61f, 0.58f);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        new Color(0.10f, 0.10f, 0.10f)
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/FacialHair_Goatee",
                        new Color(0.58f, 0.57f, 0.54f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/Overalls",
                        new Color(0.24f, 0.31f, 0.44f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(0.29f, 0.30f, 0.22f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/Beanie/Beanie",
                        new Color(0.55f, 0.18f, 0.12f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color(0.20f, 0.12f, 0.07f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/DressShoes/DressShoes",
                        new Color(0.15f, 0.10f, 0.08f)
                    );
                })
                .EnsureSupplier()
                .WithSupplierDefaults(s =>
                {
                    s.WithPersistentId(
                        SupplierPersistentId
                    )
                    .WithOrderLimits(
                        100f,
                        2000f
                    )
                    .WithDeliveryItem(
                        MollyIngredients.SafroleId
                    )
                    .WithDeliveryItem(
                        MollyIngredients.PmkId
                    )
                    .WithRecommendationMessage(
                        "Hey, I know a guy that can give you the stuff " +
                        "for a new product. He's got good stuff, maybe " +
                        "go and see him. I gave him your number, he " +
                        "should hit you up soon."
                    )
                    .WithUnlockHint(
                        "Roscoe is now available. You can order " +
                        "Safrole Oil and PMK Powder from him at Bud's Bar."
                    )
                    .WithStashDeadDrop<
                        S1API.DeadDrops.Native.GreyDocksBuilding
                    >();
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(2.5f)
                     .SetUnlocked(false)
                     .SetUnlockType(
                         NPCRelationship.UnlockType.Recommendation
                     )
                     .WithConnectionsById(
                         new[]
                         {
                             "anna_chesterfield"
                         }
                     );
                });

            if (budsBar != null)
            {
                builder.WithSchedule(plan =>
                {
                    plan.StayInBuilding(
                        budsBar,
                        0,
                        1440,
                        null,
                        null
                    );
                });

                MelonLogger.Msg(
                    "[Roscoe] Schedule assigned: " +
                    "Bud's Bar, 00:00-24:00."
                );
            }
            else
            {
                MelonLogger.Warning(
                    "[Roscoe] Bud's Bar was not found."
                );
            }

            MelonLogger.Msg(
                "[Roscoe] ConfigurePrefab completed."
            );
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            Appearance.Build();

            Aggressiveness = 0f;
            Region = Region.Docks;

            Schedule.Enable();

            Instance = this;

            // Dialogue components may not be fully initialized when
            // OnCreated runs, so registration is retried briefly.
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
                "[Roscoe] Roscoe Bellweather loaded as a supplier " +
                "at Bud's Bar."
            );
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            // DialogueHandler, DialogueController, and their container
            // list may be initialized shortly after OnCreated.
            for (int attempt = 0; attempt < 80; attempt++)
            {
                if (Dialogue != null &&
                    RoscoeDialogue.TryRegisterAndArm(Dialogue))
                {
                    MelonLogger.Msg(
                        "[Roscoe] MDMA supplier introduction is ready."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            MelonLogger.Warning(
                "[Roscoe] Timed out waiting for Roscoe's " +
                "dialogue components."
            );
        }

        private static IEnumerator
            ConfigureExistingDeadDropAfterLoad()
        {
            // Dead drops may not be registered when OnCreated fires.
            for (int attempt = 0; attempt < 80; attempt++)
            {
                if (TryConfigureExistingDeadDrop())
                {
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            MelonLogger.Warning(
                "[Roscoe] Timed out waiting for the existing " +
                "GreyDocksBuilding dead drop."
            );
        }

        private static bool TryConfigureExistingDeadDrop()
        {
            try
            {
                DeadDropInstance selectedDrop =
                    DeadDropManager.Get<
                        S1API.DeadDrops.Native.GreyDocksBuilding
                    >();

                if (selectedDrop == null)
                {
                    return false;
                }

                string selectedGuid =
                    selectedDrop.GUID;

                if (string.IsNullOrWhiteSpace(selectedGuid))
                {
                    return false;
                }

                Il2CppSystem.Collections.Generic.List<DeadDrop>
                    nativeDrops = DeadDrop.DeadDrops;

                if (nativeDrops == null ||
                    nativeDrops.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop = nativeDrops[i];

                    if (drop == null)
                    {
                        continue;
                    }

                    string runtimeGuid =
                        drop.GUID.ToString();

                    string bakedGuid =
                        drop.BakedGUID;

                    bool guidMatches =
                        string.Equals(
                            runtimeGuid,
                            selectedGuid,
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        string.Equals(
                            bakedGuid,
                            selectedGuid,
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (!guidMatches)
                    {
                        continue;
                    }

                    // This is an existing native drop. We are only
                    // renaming and re-describing it.
                    drop.DeadDropName =
                        PaymentDropName;

                    drop.DeadDropDescription =
                        PaymentDropDescription;

                    MelonLogger.Msg(
                        "[Roscoe] Existing dead drop claimed successfully: " +
                        "Name='" + drop.DeadDropName + "', " +
                        "GUID='" + selectedGuid + "'"
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Roscoe] Existing dead-drop setup failed: " +
                    ex.Message
                );
            }

            return false;
        }
    }
}