using MelonLoader;
using S1API.Economy;
using S1API.Entities;
using S1API.Entities.Customer;
using S1API.Entities.Relation;
using S1API.GameTime;
using S1API.Map;
using S1API.Products;
using S1API.Properties;
using S1API.Properties.Interfaces;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class EllenSamwell : NPC
    {
        public static EllenSamwell Instance { get; private set; }

        public const string NpcId =
            "custom_ellen_samwell";

        private static readonly Vector3 FixedPosition =
            new Vector3(
                -18.449f,
                -3.57f,
                138.357f
            );

        private static readonly Quaternion FixedRotation =
            Quaternion.Euler(
                0f,
                359.144f,
                0f
            );

        public override bool IsPhysical => true;

        protected override void ConfigurePrefab(
            NPCPrefabBuilder builder)
        {
            MelonLogger.Msg("[Ellen] ConfigurePrefab started.");

            builder
                .WithIdentity(
                    NpcId,
                    "Ellen",
                    "Samwell"
                )

                .WithSpawnPosition(
                    FixedPosition,
                    FixedRotation
                )

                .WithRegion(
                    Region.Downtown
                )

                .WithAppearanceDefaults(av =>
                {
                    // Older woman, similar age range to Dan.
                    av.Gender = 1f;
                    av.Height = 0.96f;
                    av.Weight = 0.72f;

                    av.SkinColor =
                        new Color(0.68f, 0.55f, 0.45f, 1f);

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    // Tired, pale eyes.
                    av.EyeBallTint =
                        new Color(0.74f, 0.72f, 0.66f, 1f);

                    av.PupilDilation = 0.42f;

                    // Heavy brows / tired expression.
                    av.EyebrowScale = 1.18f;
                    av.EyebrowThickness = 1.25f;
                    av.EyebrowRestingHeight = -0.22f;
                    av.EyebrowRestingAngle = -5f;

                    // Older half-lidded eyes.
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

                    // Grey bun so she reads older and distinct from Dan.
                    av.HairPath =
                        "Avatar/Hair/Bun/Bun";

                    av.HairColor =
                        new Color(0.62f, 0.61f, 0.57f, 1f);

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_Neutral",
                        new Color(0.92f, 0.82f, 0.74f, 1f)
                    );

                    // Similar blue top to Dan.
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

                // Make her a real customer so talking works.
                .EnsureCustomer()

                .WithCustomerDefaults(cd =>
                {
                    cd.WithSpending(
                          90f,
                          220f
                      )
                      .WithOrdersPerWeek(
                          1,
                          2
                      )
                      .WithPreferredOrderDay(
                          Day.Friday
                      )
                      .WithOrderTime(
                          1600
                      )
                      .WithStandards(
                          CustomerStandard.Moderate
                      )

                      // Important: allow player to talk/order directly.
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
                          0.28f
                      )
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
                              Property.Munchies
                          }
                      );
                })

                // For now, make her unlocked so talking works immediately.
                // After testing, switch this back to Jennifer recommendation
                // if desired.
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

            MelonLogger.Msg(
                "[Ellen] ConfigurePrefab completed."
            );
        }

        protected override void OnCreated()
        {
            base.OnCreated();

            Appearance.Build();

            Region = Region.Downtown;
            Aggressiveness = 0f;

            Instance = this;

            MelonCoroutines.Start(
                PlaceAtHardwareAfterLoad()
            );

            MelonLogger.Msg(
                "[Ellen] Ellen Samwell loaded as Dan's wife/customer."
            );
        }

        private static IEnumerator PlaceAtHardwareAfterLoad()
        {
            yield return new WaitForSeconds(2f);

            EllenSamwell ellen =
                Instance;

            if (ellen == null ||
                ellen.gameObject == null)
            {
                yield break;
            }

            TryWarpNpc(
                ellen.gameObject,
                FixedPosition
            );
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
                        "[Ellen] Warped to exact hardware position."
                    );

                    return;
                }
                catch (System.Exception ex)
                {
                    MelonLogger.Warning(
                        "[Ellen] Warp failed: " +
                        ex.Message
                    );
                }
            }

            MelonLogger.Warning(
                "[Ellen] No NPCMovement Warp method found."
            );
        }
    }
}