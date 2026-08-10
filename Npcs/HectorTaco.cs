using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.GameTime;
using S1API.Map;
using S1API.Map.Buildings;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using S1API.Map.Buildings;

namespace CustomNPCExample.NPCs
{
    public sealed class HectorTaco : NPC
    {
        public static HectorTaco Instance { get; private set; }

        public const string NpcId =
            "custom_hector_taco";

        private static readonly Vector3 FixedPosition =
            new Vector3(
                -28.958f,
                0.24f,
                75.044f
            );

        private static readonly Quaternion FixedRotation =
            Quaternion.Euler(
                0f,
                179.519f,
                0f
            );

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(
            NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Hector] ConfigurePrefab started.");

            builder
                .WithIdentity(
                    NpcId,
                    "Hector",
                    "Taco"
                )

                .WithSpawnPosition(
                    FixedPosition,
                    FixedRotation
                )

                .WithRegion(
                    Region.Westville
                )

                .WithAppearanceDefaults(av =>
                {
                    // Slim Taco Ticklers cashier look.
                    av.Gender = 0f;
                    av.Height = 0.98f;
                    av.Weight = 0.50f;

                    av.SkinColor =
                        new Color(0.72f, 0.52f, 0.34f, 1f);

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint =
                        new Color(0.84f, 0.80f, 0.72f, 1f);

                    av.PupilDilation = 0.55f;

                    av.EyebrowScale = 1.02f;
                    av.EyebrowThickness = 1.05f;
                    av.EyebrowRestingHeight = -0.05f;
                    av.EyebrowRestingAngle = -2f;

                    av.LeftEye =
                        new ValueTuple<float, float>(
                            0.43f,
                            0.50f
                        );

                    av.RightEye =
                        new ValueTuple<float, float>(
                            0.43f,
                            0.50f
                        );

                    // No hair so the cap does not clip.
                    av.HairPath = string.Empty;

                    av.HairColor =
                        new Color(0.90f, 0.55f, 0.10f, 1f);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        Color.white
                    );

                    // Taco Ticklers yellow shirt.
                    av.WithBodyLayer(
                        "Avatar/Layers/Top/T-Shirt",
                        new Color(0.95f, 0.73f, 0.05f, 1f)
                    );

                    // Black pants.
                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/Jeans",
                        new Color(0.04f, 0.05f, 0.07f, 1f)
                    );

                    // Taco Ticklers-style dark cap.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/Cap/Cap",
                        new Color(0.03f, 0.05f, 0.08f, 1f)
                    );

                    // Dark shoes.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Sneakers/Sneakers",
                        new Color(0.05f, 0.05f, 0.06f, 1f)
                    );

                    // Belt like the screenshot.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Waist/Belt/Belt",
                        new Color(0.02f, 0.02f, 0.025f, 1f)
                    );
                })

                // Make him interactable/customer so talking does something.
                .EnsureCustomer()

                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(
                          60f,
                          160f
                      )
                      .WithOrdersPerWeek(
                          1,
                          2
                      )
                      .WithPreferredOrderDay(
                          Day.Saturday
                      )
                      .WithOrderTime(
                          2100
                      )
                      .WithStandards(
                          CustomerStandard.Moderate
                      )
                      .AllowDirectApproach(
                          true
                      )
                      .GuaranteeFirstSample(
                          false
                      )
                      .WithMutualRelationRequirement(
                          1f,
                          3f
                      )
                      .WithCallPoliceChance(
                          0f
                      )
                      .WithDependence(
                          0.04f,
                          0.32f
                      )
                      .WithAffinities(
                          new ValueTuple<DrugType, float>[]
                          {
                              new ValueTuple<DrugType, float>(
                                  DrugType.Marijuana,
                                  0.50f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Shrooms,
                                  0.20f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Cocaine,
                                  -0.30f
                              ),

                              new ValueTuple<DrugType, float>(
                                  DrugType.Methamphetamine,
                                  -0.80f
                              )
                          }
                      )
                      .WithPreferredProperties(
                          new PropertyBase[]
                          {
                              Property.Calming,
                              Property.Munchies
                          }
                      );
                })

.WithRelationshipDefaults(r =>
{
    r.WithDelta(2.5f)
     .SetUnlocked(false)
     .SetUnlockType(
         NPCRelationship.UnlockType.Recommendation
     )
     .WithConnectionsById(new[]
     {
         "dean_webster"
     });
});

            // --- Schedule ---
            // He works the counter most of the day, then goes out.
            // StayInBuilding won't hold him at the exact counter spot,
            // so the counter shift is left unscheduled and the warp
            // in OnCreated pins him there instead.

            Building nightclub =
                Building.Get<Nightclub>();

            Building westGasMart =
                Building.Get<WestGasMart>();

            Building northApartments =
                Building.Get<NorthApartments>();

            builder.WithSchedule(plan =>
            {
                // 22:00 - 23:00 : grab a drink after shift
                if (nightclub != null)
                {
                    plan.StayInBuilding(
                        nightclub,
                        2200,
                        60,
                        null,
                        null
                    );
                }

                // 23:00 - 00:00 : late night snack run
                if (westGasMart != null)
                {
                    plan.StayInBuilding(
                        westGasMart,
                        2300,
                        60,
                        null,
                        null
                    );
                }

                // 00:00 - 08:00 : home / sleep
                if (northApartments != null)
                {
                    plan.StayInBuilding(
                        northApartments,
                        0,
                        480,
                        null,
                        null
                    );
                }
            });

            MelonLogger.Msg(
                "[Hector] Schedule assigned: " +
                "counter 08:00-22:00, out 22:00-00:00, home 00:00-08:00."
            );

            MelonLogger.Msg(
                "[Hector] ConfigurePrefab completed."
            );
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            Appearance.Build();

            Region = Region.Westville;
            Aggressiveness = 0f;

            Instance = this;

            MelonCoroutines.Start(
                KeepAtCounterDuringShift()
            );

            MelonLogger.Msg(
                "[Hector] Hector Taco loaded at Taco Ticklers."
            );
        }

        private static IEnumerator KeepAtCounterDuringShift()
        {
            yield return new WaitForSeconds(2f);

            while (true)
            {
                HectorTaco hector = Instance;

                if (hector == null ||
                    hector.gameObject == null)
                {
                    yield return new WaitForSeconds(5f);
                    continue;
                }

                int currentTime =
                    S1API.GameTime.TimeManager.CurrentTime;

                bool onShift =
                    currentTime >= 800 &&
                    currentTime < 2200;

                if (onShift)
                {
                    float distance =
                        Vector3.Distance(
                            hector.gameObject.transform.position,
                            FixedPosition
                        );

                    // Only re-warp if he has drifted off the spot.
                    if (distance > 1.5f)
                    {
                        TryWarpNpc(
                            hector.gameObject,
                            FixedPosition
                        );
                    }
                }

                yield return new WaitForSeconds(10f);
            }
        }

        private static void TryWarpNpc(
            GameObject npcObject,
            Vector3 position)
        {
            Component[] components =
                npcObject.GetComponentsInChildren<Component>(
                    true
                );

            for (int i = 0; i < components.Length; i++)
            {
                Component component =
                    components[i];

                if (component == null)
                    continue;

                Type type =
                    component.GetType();

                string typeName =
                    type.Name;

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

                    MelonLogger.Msg(
                        "[Hector] Warped to Taco Ticklers counter."
                    );

                    return;
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Warning(
                        "[Hector] Warp failed: " +
                        ex.Message
                    );
                }
            }

            MelonLogger.Warning(
                "[Hector] No NPCMovement Warp method found."
            );
        }
    }
}