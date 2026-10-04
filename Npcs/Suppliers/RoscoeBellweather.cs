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
            global::CustomNPCExample.Utils.WvcLog.Msg("[Roscoe] ConfigurePrefab started.");

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
                    Region.Downtown
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
                            // Jessi Waters knows him from the bar circuit.
                            "jessi_waters"
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Schedule assigned: Bud's Bar, 00:00-24:00."
                );
            }
            else
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
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

                Aggressiveness = 1f;
                Region = Region.Northtown;

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

                global::CustomNPCExample.Utils.WvcLog.Msg(
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

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Roscoe] Idle schedule disabled for native meetup."
                );
            }
            catch (Exception)
            {

            }
        }

        public static void ResumeIdleScheduleAfterBulkMeeting()
        {
            if (Instance == null)
                return;

            MelonCoroutines.Start(Instance.ResumeScheduleRoutine());
        }

        private IEnumerator ResumeScheduleRoutine()
        {
            TryWarpHome();
            global::CustomNPCExample.Utils.WvcLog.Msg("[Roscoe] Teleported Roscoe home before enabling schedule.");

            yield return new WaitForSeconds(0.5f);

            try
            {
                Schedule.Enable();
                _scheduleSuspendedForBulkMeeting = false;
                global::CustomNPCExample.Utils.WvcLog.Msg("[Roscoe] Schedule enabled safely at destination.");
            }
            catch (Exception)
            {

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

        private IEnumerator ConfigureBulkMeetupsAfterLoad()
        {
            for (int attempt = 0;
                 attempt < 80;
                 attempt++)
            {
                if (RoscoeBulkMeetupManager.TryRegister(this))
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Roscoe] Bulk meetup system ready."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(
                    0.25f
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
                    RoscoeDialogue.TryRegisterAndArm(
                        Dialogue
                    ))
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Roscoe] MDMA supplier introduction is ready."
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

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[Roscoe] Dead drop claimed: " +
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
