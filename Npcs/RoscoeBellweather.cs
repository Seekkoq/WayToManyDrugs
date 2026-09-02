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
using System.Reflection;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class RoscoeBellweather : NPC
    {
        public static RoscoeBellweather Instance { get; private set; }

        public const string NpcId = "custom_roscoe_bellweather";
        public const string SupplierPersistentId = "wvc_roscoe_bellweather_v2";

        public static readonly Vector3 HomePosition =
            new Vector3(
                -72f,
                -0.76f,
                -58f
            );

        public static readonly Quaternion HomeRotation =
            Quaternion.identity;

        private static bool _scheduleSuspendedForBulkMeeting;

        private const string PaymentDropName = "Roscoe's Payment Drop";

        private const string PaymentDropDescription =
            "Roscoe's payment drop. Leave what you owe here. " +
            "He'll know when the cash lands.";

        private bool _dialogueSetupStarted;
        private bool _deadDropSetupStarted;
        private bool _bulkMeetupSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Roscoe] ConfigurePrefab started.");

            Building budsBar = NPCBuildingLookup.Get("Bud's Bar");

            builder
                .WithIdentity(
                    NpcId,
                    "Roscoe",
                    "Bellweather"
                )
                .WithSpawnPosition(
                    HomePosition,
                    HomeRotation
                )
                .WithRegion(
                    Region.Docks
                )
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.94f;
                    av.Weight = 0.88f;

                    av.SkinColor =
                        new Color32(
                            199,
                            153,
                            125,
                            255
                        );

                    av.LeftEyeLidColor =
                        av.SkinColor;

                    av.RightEyeLidColor =
                        av.SkinColor;

                    av.EyeBallTint =
                        new Color(
                            0.82f,
                            0.84f,
                            0.80f
                        );

                    av.PupilDilation = 0.38f;

                    av.EyebrowScale = 1.24f;
                    av.EyebrowThickness = 1.40f;
                    av.EyebrowRestingHeight = -0.34f;
                    av.EyebrowRestingAngle = -9f;

                    av.HairPath = string.Empty;

                    av.HairColor =
                        new Color32(
                            158,
                            156,
                            148,
                            255
                        );

                    av.WithFaceLayer<Face>(
                        Face.Neutral,
                        new Color(
                            0.10f,
                            0.10f,
                            0.10f
                        )
                    );

                    av.WithBodyLayer<Shirts>(
                        Shirts.Overalls,
                        new Color32(
                            61,
                            79,
                            112,
                            255
                        )
                    );

                    av.WithBodyLayer<Pants>(
                        Pants.Jeans,
                        new Color32(
                            74,
                            77,
                            56,
                            255
                        )
                    );

                    av.WithAccessoryLayer<Head>(
                        Head.Beanie,
                        new Color32(
                            140,
                            46,
                            31,
                            255
                        )
                    );

                    av.WithAccessoryLayer<Waist>(
                        Waist.Belt,
                        new Color32(
                            51,
                            31,
                            18,
                            255
                        )
                    );

                    av.WithAccessoryLayer<Feet>(
                        Feet.DressShoes,
                        new Color32(
                            38,
                            26,
                            20,
                            255
                        )
                    );
                })
                .EnsureSupplier()
                .WithSupplierDefaults(s => s
                    .WithPersistentId(SupplierPersistentId)
                    .WithOrderLimits(100f, 15000f)
                    .WithStashDeadDrop<GreyDocksBuilding>()

                    // Shop stock — these appear in Roscoe's meetup shop
                    .WithDeliveryItem(MollyIngredients.SafroleId)
                    .WithDeliveryItem(MollyIngredients.PmkId)
                    .WithDeliveryItem(MollyIngredients.PmkRefinedId)
                    .WithDeliveryItem(MollyIngredients.PmkLabGradeId)

                    .WithRecommendationMessage(
                        "Hey, I know a guy that can give you the stuff " +
                        "for a new product. He's got good stuff, maybe " +
                        "go and see him. I gave him your number, he " +
                        "should hit you up soon.")
                    .WithUnlockHint(
                        "Roscoe is now available. Find him at Bud's Bar."))
                .WithRelationshipDefaults(r => r
                    .WithDelta(
                        3f
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
                    "[Roscoe] Schedule assigned: Bud's Bar, 00:00-24:00."
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
            try
            {
                base.OnCreated();

                Instance = this;
                _scheduleSuspendedForBulkMeeting = false;

                Aggressiveness = 0f;
                Region = Region.Docks;

                Appearance.Build();
                Schedule.Enable();

                try
                {
                    var s = gameObject?.GetComponent<Il2CppScheduleOne.Economy.Supplier>()
                        ?? gameObject?.GetComponentInChildren<Il2CppScheduleOne.Economy.Supplier>(true);
                    if (s != null)
                    {
                        s.DeliveriesEnabled = false;
                    }
                }
                catch { }

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

                if (!_bulkMeetupSetupStarted)
                {
                    _bulkMeetupSetupStarted = true;

                    MelonCoroutines.Start(
                        ConfigureBulkMeetupsAfterLoad()
                    );
                }

                MelonLogger.Msg(
                    "[Roscoe] Roscoe Bellweather loaded as a supplier at Bud's Bar."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[Roscoe] OnCreated failed: " +
                    ex
                );
            }
        }

        // ------------------------------------------------------------
        // Safe Warp-then-Enable Sequence
        // ------------------------------------------------------------

        public static void SuspendIdleScheduleForBulkMeeting()
        {
            if (Instance == null)
                return;

            if (_scheduleSuspendedForBulkMeeting)
                return;

            try
            {
                Instance.Schedule.Disable();
                _scheduleSuspendedForBulkMeeting = true;

                MelonLogger.Msg(
                    "[Roscoe] Idle schedule disabled for native meetup."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Roscoe] Could not suspend idle schedule: " +
                    ex.Message
                );
            }
        }

        public static void ResumeIdleScheduleAfterBulkMeeting()
        {
            if (Instance == null)
                return;

            // Run the safe warp-then-enable sequence inside a frame-delayed routine
            MelonCoroutines.Start(Instance.ResumeScheduleRoutine());
        }

        private IEnumerator ResumeScheduleRoutine()
        {
            // 1. Teleport him home while the schedule is still completely DISABLED
            TryWarpHome();
            MelonLogger.Msg("[Roscoe] Teleported Roscoe home before enabling schedule.");

            // 2. Wait half a second for physics and transform updates to settle on the server
            yield return new WaitForSeconds(0.5f);

            // 3. Re-enable his StayInBuilding schedule now that he is already at Bud's Bar
            try
            {
                Schedule.Enable();
                _scheduleSuspendedForBulkMeeting = false;
                MelonLogger.Msg("[Roscoe] Schedule enabled safely at destination.");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Roscoe] Could not enable schedule: " +
                    ex.Message
                );
            }
        }

        public static bool TryWarpHome()
        {
            return TryWarp(HomePosition, HomeRotation);
        }

        public static bool TryWarp(Vector3 targetPosition)
        {
            if (Instance == null || Instance.gameObject == null)
                return false;
            return TryWarp(targetPosition, Instance.gameObject.transform.rotation);
        }

        public static bool TryWarp(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (Instance == null ||
                Instance.gameObject == null)
            {
                return false;
            }

            GameObject npcObject =
                Instance.gameObject;

            Quaternion rot = targetRotation;

            try
            {
                Component[] components =
                    npcObject.GetComponentsInChildren<Component>(
                        true
                    );

                for (int i = 0;
                     i < components.Length;
                     i++)
                {
                    Component component =
                        components[i];

                    if (component == null)
                        continue;

                    Type type =
                        component.GetType();

                    string typeName =
                        type.Name ?? string.Empty;

                    if (typeName.IndexOf(
                            "Movement",
                            StringComparison.OrdinalIgnoreCase
                        ) < 0)
                    {
                        continue;
                    }

                    MethodInfo warp =
                        type.GetMethod(
                            "Warp",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic,
                            null,
                            new[]
                            {
                                typeof(Vector3)
                            },
                            null
                        );

                    if (warp == null)
                        continue;

                    try
                    {
                        warp.Invoke(
                            component,
                            new object[]
                            {
                                targetPosition
                            }
                        );

                        npcObject.transform.rotation = rot;

                        return true;
                    }
                    catch { }
                }
            }
            catch { }

            try
            {
                npcObject.transform.position =
                    targetPosition;

                npcObject.transform.rotation =
                    rot;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool RequestBulkMeetup()
        {
            return RoscoeBulkMeetupManager.RequestBulkMeetup();
        }

        // ------------------------------------------------------------
        // Setup Coroutines
        // ------------------------------------------------------------

        private IEnumerator ConfigureBulkMeetupsAfterLoad()
        {
            for (int attempt = 0;
                 attempt < 80;
                 attempt++)
            {
                if (RoscoeBulkMeetupManager.TryRegister(this))
                {
                    MelonLogger.Msg(
                        "[Roscoe] Bulk meetup system ready."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(
                    0.25f
                );
            }

            MelonLogger.Warning(
                "[Roscoe] Timed out waiting for native Supplier component."
            );
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0;
                 attempt < 60;
                 attempt++)
            {
                if (Dialogue != null &&
                    RoscoeDialogue.TryRegisterAndArm(
                        Dialogue
                    ))
                {
                    MelonLogger.Msg(
                        "[Roscoe] MDMA supplier introduction is ready."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(
                    0.25f
                );
            }

            MelonLogger.Warning(
                "[Roscoe] Timed out waiting for dialogue components."
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
                "[Roscoe] Timed out waiting for dead drop."
            );
        }

        private static bool TryConfigureExistingDeadDrop()
        {
            try
            {
                DeadDropInstance selectedDrop =
                    DeadDropManager.Get<
                        GreyDocksBuilding
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
                        "[Roscoe] Dead drop claimed: " +
                        guid
                    );

                    return true;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(
                    "[Roscoe] Dead drop setup failed: " +
                    ex.Message
                );
            }

            return false;
        }
    }
}