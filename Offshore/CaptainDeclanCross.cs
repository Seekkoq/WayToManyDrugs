using System;
using System.Collections;
using MelonLoader;
using S1API.Entities;
using S1API.Entities.Appearances.AccessoryFields;
using S1API.Entities.Appearances.BodyLayerFields;
using S1API.Entities.Appearances.FaceLayerFields;
using S1API.Entities.Voices;
using S1API.Map;
using S1API.Map.Buildings;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public sealed class CaptainDeclanCross : NPC
    {
        public static CaptainDeclanCross Instance { get; private set; }

        public const string NpcId = "custom_declan_cross";

        public static readonly Vector3 DefaultHomePosition =
            new Vector3(-84.807f, -2.36f, -26.24f);

        public static readonly Quaternion DefaultHomeRotation =
            Quaternion.Euler(0f, 116.95f, 0f);

        public override bool IsPhysical => true;

        private bool _dialogueArmed;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            Instance = this;

            Building budsBar = NPCBuildingLookup.Get("Bud's Bar");
            Building pawnShop = NPCBuildingLookup.Get("Pawn Shop");
            Building motel = NPCBuildingLookup.Get("Motel Office");
            Building gasMart = NPCBuildingLookup.Get("West Gas-Mart");
            Building fallback = budsBar ?? pawnShop ?? motel ?? gasMart;

            builder
                .WithIdentity(NpcId, "Declan", "Cross")
                .WithVoice(NPCVoiceCatalog.Cold, 0.92f)
                .WithSpawnPosition(DefaultHomePosition, DefaultHomeRotation)
                .WithRegion(Region.Downtown)
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 1.03f;
                    av.Weight = 0.86f;

                    av.SkinColor = new Color32(205, 160, 130, 255);
                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;
                    av.EyeBallTint = new Color(0.88f, 0.90f, 0.86f);
                    av.PupilDilation = 0.42f;

                    av.EyebrowScale = 1.15f;
                    av.EyebrowThickness = 1.30f;
                    av.EyebrowRestingHeight = -0.15f;
                    av.EyebrowRestingAngle = -4f;

                    av.HairColor = new Color32(110, 105, 100, 255);
                    av.HairPath = "Avatar/Hair/Receding/Receding";

                    av.WithFaceLayer("Avatar/Layers/Face/Face_SmugPout", new Color(0.12f, 0.10f, 0.08f, 1f));
                    av.WithFaceLayer("Avatar/Layers/Face/FacialHair_Stubble", new Color32(60, 55, 50, 180));
                    av.WithFaceLayer("Avatar/Layers/Face/OldPersonWrinkles", new Color32(140, 95, 75, 120));

                    av.WithBodyLayer("Avatar/Layers/Top/RolledButtonup", new Color32(42, 60, 85, 255));
                    av.WithBodyLayer("Avatar/Layers/Bottom/CargoPants", new Color32(55, 60, 48, 255));

                    av.WithAccessoryLayer("Avatar/Accessories/Head/FlatCap/FlatCap", new Color32(40, 40, 42, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Chest/CollarJacket/CollarJacket", new Color32(80, 52, 38, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Feet/CombatBoots/CombatBoots", new Color32(28, 25, 24, 255));
                    av.WithAccessoryLayer("Avatar/Accessories/Waist/Belt/Belt", new Color32(45, 30, 20, 255));
                })
                .WithRelationshipDefaults(r =>
                {
                    r.WithDelta(2.5f)
                     .SetUnlocked(true);
                })
                .WithSchedule(plan =>
                {
                    if (gasMart != null)
                        plan.StayInBuilding(gasMart, 600, 300, null, "Declan - Getting provisions");

                    if (budsBar != null)
                        plan.StayInBuilding(budsBar, 1100, 300, null, "Declan - Meeting overseas smuggling contacts");

                    if (pawnShop != null)
                        plan.StayInBuilding(pawnShop, 1600, 300, null, "Declan - Checking overseas gear");

                    if (motel != null)
                        plan.StayInBuilding(motel, 2100, 540, null, "Declan - Resting between voyages");
                });

        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();
                Instance = this;

                Appearance.Build();
                Aggressiveness = 0f;
                Region = Region.Downtown;
                Schedule.Enable();

                MelonCoroutines.Start(ConfigureDialogueAfterLoad());

            }
            catch (Exception ex)
            {
                MelonLogger.Error("[Offshore] Captain Declan Cross OnCreated exception: " + ex);
            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (Dialogue != null && OffshoreDialogue.Register(Dialogue))
                {
                    _dialogueArmed = true;
                    yield break;
                }
                yield return new WaitForSeconds(0.25f);
            }


        }

        public void EnsureDialogueArmed()
        {
            if (_dialogueArmed)
                return;

            try
            {
                if (Dialogue != null && OffshoreDialogue.Register(Dialogue))
                {
                    _dialogueArmed = true;
                }
            }
            catch (Exception)
            {

            }
        }

        private static float _dockWalkStartTime = -1f;
        private static float _lastWalkIssueTime = -999f;

        public static void WalkToDock()
        {
            var inst = Instance;
            if (inst == null)
                return;

            try
            {
                var movement = inst.Movement;
                float dist = Vector3.Distance(inst.Position, OffshoreSalesManager.DockSpawnPosition);

                if (dist <= 1.5f)
                {
                    _dockWalkStartTime = -1f;
                    movement?.Stop();
                    movement?.FaceDirection(OffshoreSalesManager.DockSpawnRotation * Vector3.forward);
                    return;
                }

                if (movement == null)
                {
                    TryWarp(OffshoreSalesManager.DockSpawnPosition, OffshoreSalesManager.DockSpawnRotation);
                    return;
                }

                bool headingToDock =
                    movement.IsMoving &&
                    Vector3.Distance(movement.CurrentDestination, OffshoreSalesManager.DockSpawnPosition) <= 1.0f;

                if (!headingToDock && Time.time - _lastWalkIssueTime > 2f)
                {
                    movement.SetDestination(OffshoreSalesManager.DockSpawnPosition);
                    _lastWalkIssueTime = Time.time;

                    if (_dockWalkStartTime < 0f)
                    {
                        _dockWalkStartTime = Time.time;
                    }
                }

                if (_dockWalkStartTime >= 0f && Time.time - _dockWalkStartTime > 30f)
                {

                    _dockWalkStartTime = -1f;
                    TryWarp(OffshoreSalesManager.DockSpawnPosition, OffshoreSalesManager.DockSpawnRotation);
                }
            }
            catch (Exception)
            {

                TryWarp(OffshoreSalesManager.DockSpawnPosition, OffshoreSalesManager.DockSpawnRotation);
                _dockWalkStartTime = -1f;
            }
        }

        public static void SuspendSchedule()
        {
            try
            {
                Instance?.Schedule?.Disable();
            }
            catch { }
        }

        public static void ResumeSchedule()
        {
            try
            {
                Instance?.Schedule?.Enable();
            }
            catch { }
        }

        public static bool TryWarp(Vector3 position, Quaternion rotation)
        {
            if (Instance == null)
                return false;

            try
            {
                Instance.Position = position;

                if (Instance.Movement != null)
                {
                    Instance.Movement.Warp(position);
                    Instance.Movement.FaceDirection(rotation * Vector3.forward);
                }

                if (Instance.gameObject != null)
                {
                    Instance.gameObject.transform.position = position;
                    Instance.gameObject.transform.rotation = rotation;
                }

                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
