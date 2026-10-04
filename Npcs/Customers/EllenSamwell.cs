using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.Entities.Voices;
using S1API.GameTime;
using S1API.Map;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using S1API.Entities.Voices;

namespace CustomNPCExample.NPCs
{
    public sealed class EllenSamwell : NPC
    {
        public static EllenSamwell Instance { get; private set; }

        public const string NpcId =
            "custom_ellen_samwell";

        private static readonly Vector3 DayPosition =
            new Vector3(
                -18.449f,
                -3.57f,
                138.357f
            );

        private static readonly Quaternion DayRotation =
            Quaternion.Euler(
                0f,
                359.144f,
                0f
            );

        private static readonly Vector3 NightPosition =
            new Vector3(
                -18.449f,
                -2.00f,
                138.357f
            );

        private static readonly Quaternion NightRotation =
            Quaternion.Euler(
                0f,
                180f,
                0f
            );

        private const int MorningStart = 700;
        private const int EveningStart = 1800;

        private bool _watcherStarted;
        private bool _isNightPlacement;
        private bool _hasPlacement;

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(
            NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[Ellen] ConfigurePrefab started.");

            builder
                .WithIdentity(
                    NpcId,
                    "Ellen",
                    "Samwell"
                )
                .WithVoice(NPCVoiceCatalog.Female1, 1.0f)
                .WithSpawnPosition(
                    DayPosition,
                    DayRotation
                )
                .WithRegion(
                    Region.Downtown
                )
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 1f;
                    av.Height = 0.96f;
                    av.Weight = 0.72f;

                    av.SkinColor =
                        new Color(0.68f, 0.55f, 0.45f, 1f);

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint =
                        new Color(0.74f, 0.72f, 0.66f, 1f);

                    av.PupilDilation = 0.42f;

                    av.EyebrowScale = 1.18f;
                    av.EyebrowThickness = 1.25f;
                    av.EyebrowRestingHeight = -0.22f;
                    av.EyebrowRestingAngle = -5f;

                    av.LeftEye =
                        new ValueTuple<float, float>(0.34f, 0.46f);

                    av.RightEye =
                        new ValueTuple<float, float>(0.34f, 0.46f);

                    av.HairPath =
                        "Avatar/Hair/Bun/Bun";

                    av.HairColor =
                        new Color(0.62f, 0.61f, 0.57f, 1f);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        new Color(0.92f, 0.82f, 0.74f, 1f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/ButtonUp",
                        new Color(0.23f, 0.46f, 0.78f, 1f)
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(0.07f, 0.09f, 0.15f, 1f)
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/DressShoes/DressShoes",
                        new Color(0.06f, 0.06f, 0.07f, 1f)
                    );
                })
                .EnsureCustomer()
                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(90f, 220f)
                      .WithOrdersPerWeek(1, 2)
                      .WithPreferredOrderDay(Day.Friday)
                      .WithOrderTime(1600)
                      .WithStandards(CustomerStandard.Moderate)
                      .AllowDirectApproach(true)
                      .GuaranteeFirstSample(false)
                      .WithMutualRelationRequirement(1f, 3f)
                      .WithCallPoliceChance(0f)
                      .WithDependence(0.04f, 0.28f)
                      .WithAffinities(
                          new ValueTuple<DrugType, float>[]
                          {
                              new ValueTuple<DrugType, float>(
                                  DrugType.Marijuana,
                                  0.40f
                              ),
                              new ValueTuple<DrugType, float>(
                                  DrugType.Shrooms,
                                  -0.10f
                              ),
                              new ValueTuple<DrugType, float>(
                                  DrugType.Cocaine,
                                  -0.70f
                              ),
                              new ValueTuple<DrugType, float>(
                                  DrugType.Methamphetamine,
                                  -0.95f
                              )
                          }
                      )
                      .WithPreferredProperties(
                            new PropertyBase[]
                            {
                                Property.Calming,
                                Property.Glowie
                            }
                        );
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(2f)
                     .SetUnlocked(true)
                     .SetUnlockType(
                         NPCRelationship.UnlockType.DirectApproach
                     )
                     .WithConnectionsById(
                         new[]
                         {
                             "jennifer_rivera"
                         }
                     );
                });

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Ellen] ConfigurePrefab completed (manual day/night placement)."
            );
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            Appearance.Build();

            try
            {
                Schedule.Disable();
            }
            catch (Exception)
            {

            }

            ClearScheduleEvents();

            Region = Region.Downtown;
            Aggressiveness = 0f;

            Instance = this;

            if (!_watcherStarted)
            {
                _watcherStarted = true;
                MelonCoroutines.Start(DailyPlacementRoutine());
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[Ellen] Ellen Samwell loaded as Dan's wife/customer."
            );
        }

        private void ClearScheduleEvents()
        {
            try
            {
                object schedule = Schedule;

                if (schedule == null)
                    return;

                string[] memberNames =
                {
            "Events",
            "events",
            "_events",
            "ScheduleEvents",
            "scheduleEvents",
            "_scheduleEvents"
        };

                for (int i = 0; i < memberNames.Length; i++)
                {
                    object events =
                        GetMemberValue(
                            schedule,
                            memberNames[i]
                        );

                    if (events == null)
                        continue;

                    MethodInfo clear =
                        events.GetType().GetMethod(
                            "Clear",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic
                        );

                    if (clear != null &&
                        clear.GetParameters().Length == 0)
                    {
                        clear.Invoke(events, null);

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "[Ellen] Cleared schedule events via '" +
                            memberNames[i] +
                            "'."
                        );

                        return;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private IEnumerator DailyPlacementRoutine()
        {
            yield return new WaitForSeconds(3f);

            while (true)
            {
                EllenSamwell ellen = Instance;

                if (ellen != null &&
                    ellen.gameObject != null)
                {
                    bool night = IsNightTime();

                    if (!_hasPlacement ||
                        night != _isNightPlacement)
                    {
                        if (night)
                        {
                            PlaceAt(
                                NightPosition,
                                NightRotation,
                                "night / upstairs"
                            );
                        }
                        else
                        {
                            PlaceAt(
                                DayPosition,
                                DayRotation,
                                "day / outdoor"
                            );
                        }

                        _isNightPlacement = night;
                        _hasPlacement = true;
                    }
                }

                yield return new WaitForSeconds(10f);
            }
        }

        private static bool IsNightTime()
        {
            int time = GetCurrentMilitaryTime();

            if (time < 0)
            {
                return false;
            }

            return time >= EveningStart ||
                   time < MorningStart;
        }

        private static int GetCurrentMilitaryTime()
        {
            try
            {
                Type timeManagerType =
                    Type.GetType(
                        "S1API.GameTime.TimeManager, S1API"
                    )
                    ?? Type.GetType(
                        "Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp"
                    );

                if (timeManagerType == null)
                    return -1;

                PropertyInfo instanceProp =
                    timeManagerType.GetProperty(
                        "Instance",
                        BindingFlags.Public | BindingFlags.Static
                    );

                object instance =
                    instanceProp?.GetValue(null);

                if (instance == null)
                    return -1;

                string[] names =
                {
                    "CurrentTime",
                    "Time",
                    "CurrentTimeInt",
                    "MinuteOfDay"
                };

                foreach (string name in names)
                {
                    PropertyInfo prop =
                        timeManagerType.GetProperty(
                            name,
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );

                    if (prop != null)
                    {
                        int converted =
                            ConvertToMilitary(
                                prop.GetValue(instance),
                                name
                            );

                        if (converted >= 0)
                            return converted;
                    }

                    FieldInfo field =
                        timeManagerType.GetField(
                            name,
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance
                        );

                    if (field != null)
                    {
                        int converted =
                            ConvertToMilitary(
                                field.GetValue(instance),
                                name
                            );

                        if (converted >= 0)
                            return converted;
                    }
                }
            }
            catch
            {
            }

            return -1;
        }

        private static int ConvertToMilitary(
            object value,
            string sourceName)
        {
            if (value == null)
                return -1;

            try
            {
                if (value is int intValue)
                {
                    if (string.Equals(
                            sourceName,
                            "MinuteOfDay",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        int hour = intValue / 60;
                        int minute = intValue % 60;
                        return (hour * 100) + minute;
                    }

                    if (intValue >= 0 && intValue <= 2359)
                        return intValue;
                }

                if (value is float floatValue)
                {
                    int mins = Mathf.RoundToInt(floatValue);
                    int hour = mins / 60;
                    int minute = mins % 60;

                    if (hour < 24)
                        return (hour * 100) + minute;
                }
            }
            catch
            {
            }

            return -1;
        }

        private static void PlaceAt(
            Vector3 position,
            Quaternion rotation,
            string reason)
        {
            EllenSamwell ellen = Instance;

            if (ellen == null ||
                ellen.gameObject == null)
            {
                return;
            }

            if (TryWarpNpc(
                    ellen.gameObject,
                    position,
                    rotation))
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Ellen] Placed via Warp (" + reason + "): " +
                    position
                );

                return;
            }

            try
            {
                ellen.gameObject.transform.position = position;
                ellen.gameObject.transform.rotation = rotation;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[Ellen] Placed via transform (" + reason + "): " +
                    position
                );
            }
            catch (Exception)
            {

            }
        }

        private static bool TryWarpNpc(
            GameObject npcObject,
            Vector3 position,
            Quaternion rotation)
        {
            Component[] components =
                npcObject.GetComponentsInChildren<Component>(true);

            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];

                if (component == null)
                    continue;

                Type type = component.GetType();
                string typeName = type.Name;

                if (typeName != "NPCMovement" &&
                    !typeName.Contains("Movement"))
                {
                    continue;
                }

                MethodInfo warp =
                    type.GetMethod(
                        "Warp",
                        new[]
                        {
                            typeof(Vector3)
                        }
                    );

                if (warp == null)
                    continue;

                try
                {
                    warp.Invoke(
                        component,
                        new object[]
                        {
                            position
                        }
                    );

                    npcObject.transform.rotation = rotation;
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null || string.IsNullOrEmpty(name))
                return null;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (property != null && property.GetIndexParameters().Length == 0)
                        return property.GetValue(target);
                }
                catch { }

                try
                {
                    FieldInfo field = type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                    if (field != null)
                        return field.GetValue(target);
                }
                catch { }

                type = type.BaseType;
            }

            return null;
        }
    }
}
