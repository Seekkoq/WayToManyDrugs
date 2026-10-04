using CustomNPCExample.Products;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using S1API.DeadDrops;
using S1API.DeadDrops.Native;
using S1API.Entities;
using S1API.Entities.Relation;
using S1API.Map;
using System;
using System.Collections;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    /// <summary>
    /// Dr. Eleanor "Pill" Scrivens, the corrupt pharmacist at Pillville.
    ///
    /// She skims unpressed Xanax powder out of the pharmacy's shipments and moves it through the
    /// native supplier order flow, taking payment at the dead drop behind the medical practice.
    /// The finished bars are never stocked: pressing the powder is the player's own job. Pillville
    /// is a closed storefront with no interior, so she works the pavement out front and the
    /// schedule keeps her on site all day instead of sending the player chasing a timetable.
    /// </summary>
    public sealed class PillVilleSupplier : NPC
    {
        public static PillVilleSupplier Instance { get; private set; }

        public const string NpcId = "custom_pill_ville";
        public const string SupplierPersistentId = "wvc_pill_ville_v1";

        private const string PaymentDropName = "Pillville Stash";

        private const string PaymentDropDescription =
            "A taped-up pouch behind the medical practice. Leave the cash wrapped.";

        /// <summary>
        /// Pillville's own transform, read out of the exported main scene: the "Pharmacy" object
        /// under Region_Northtown, sitting at 90 degrees of yaw. The sign object hangs off it, so
        /// this point is the building itself rather than the street outside.
        /// </summary>
        private static readonly Vector3 DefaultSpawnPosition =
            new Vector3(-51.75f, -4.10f, 121.0f);

        private static readonly Quaternion DefaultSpawnRotation =
            Quaternion.Euler(0f, 90f, 0f);

        private bool _dialogueSetupStarted;
        private bool _deadDropSetupStarted;

        public override bool IsPhysical => true;
        public override bool IsSupplier => true;

        protected override void ConfigurePrefab(NPCPrefabBuilder builder)
        {
            global::CustomNPCExample.Utils.WvcLog.Msg("[PillVille] ConfigurePrefab started.");

            Building pillville = NPCBuildingLookup.Get("Pillville");
            Building cornerStore = NPCBuildingLookup.Get("Corner Store");
            Building nightStop = NPCBuildingLookup.Get("Apartment Building");

            if (pillville == null)
            {

            }

            if (cornerStore == null)
            {

            }

            builder
                .WithIdentity(
                    NpcId,
                    "Eleanor",
                    "Scrivens"
                )
                .WithRegion(
                    Region.Northtown
                )
                .WithSpawnPosition(
                    DefaultSpawnPosition,
                    DefaultSpawnRotation
                )
                .WithAppearanceDefaults(av =>
                {
                    av.Gender = 0f;
                    av.Height = 0.97f;
                    av.Weight = 0.58f;

                    av.SkinColor =
                        new Color32(
                            226,
                            190,
                            166,
                            255
                        );

                    av.LeftEyeLidColor = av.SkinColor;
                    av.RightEyeLidColor = av.SkinColor;

                    av.EyeBallTint =
                        new Color(
                            0.78f,
                            0.82f,
                            0.86f
                        );

                    av.PupilDilation = 0.5f;

                    av.EyebrowScale = 1.02f;
                    av.EyebrowThickness = 1.05f;
                    av.EyebrowRestingHeight = -0.22f;
                    av.EyebrowRestingAngle = -6f;

                    av.LeftEye = new ValueTuple<float, float>(0.46f, 0.52f);
                    av.RightEye = new ValueTuple<float, float>(0.46f, 0.52f);

                    av.HairPath =
                        "Avatar/Hair/Bun/Bun";

                    av.HairColor =
                        new Color32(
                            96,
                            58,
                            40,
                            255
                        );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/Face_SmugPout",
                        new Color(
                            0.12f,
                            0.10f,
                            0.10f
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/EyeShadow",
                        new Color32(
                            70,
                            60,
                            66,
                            150
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/OldPersonWrinkles",
                        new Color32(
                            180,
                            140,
                            120,
                            90
                        )
                    );

                    av.WithFaceLayer(
                        "Avatar/Layers/Face/TiredEyes",
                        new Color(
                            0.38f,
                            0.26f,
                            0.28f,
                            0.55f
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Top/Buttonup",
                        new Color32(
                            236,
                            236,
                            232,
                            255
                        )
                    );

                    av.WithBodyLayer(
                        "Avatar/Layers/Bottom/MediumSkirt",
                        new Color32(
                            58,
                            62,
                            76,
                            255
                        )
                    );

                    // A skirt is two assets in this game: the body layer paints the fabric onto the
                    // legs and the accessory mesh is the skirt itself. Only the layer was set
                    // before, which left her legs bare under the paint - hence the missing pants.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Bottom/MediumSkirt/MediumSkirt",
                        new Color32(
                            58,
                            62,
                            76,
                            255
                        )
                    );

                    // The coat does the whole read: white blazer, thin glasses, sensible flats.
                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Chest/Blazer/Blazer",
                        new Color32(
                            244,
                            244,
                            240,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Head/RectangleFrameGlasses/RectangleFrameGlasses",
                        new Color32(
                            118,
                            92,
                            54,
                            255
                        )
                    );

                    av.WithAccessoryLayer(
                        "Avatar/Accessories/Feet/Flats/Flats",
                        new Color32(
                            44,
                            40,
                            38,
                            255
                        )
                    );
                })
                .WithSupplierDefaults(s => s
                    .WithPersistentId(
                        SupplierPersistentId
                    )
                    .WithOrderLimits(
                        80f,
                        8000f
                    )
                    .WithStashDeadDrop<
                        BehindMedicalPractice
                    >()

                    // She only sells the raw powder. The bar is the player's product - a Brick Press
                    // turns her powder into Xanax - so the finished bar is deliberately never
                    // stocked here and the recipe is the only way to get it. The press itself is not
                    // sold here either; somebody else in town handles the equipment.
                    .WithDeliveryItem(
                        XanaxPowder.ItemId
                    )

                    .WithRecommendationMessage(
                        "It's Charles Rowland. There's a pharmacist over at Pillville who " +
                        "keeps back-stock for people she likes - Scrivens. She works the corner " +
                        "store in Westville these days, so look for her there."
                    )

                    .WithUnlockHint(
                        "Scrivens is on your supplier list. Order Xanax powder through " +
                        "your phone; payment goes to the stash behind the medical practice."
                    )
                )
                .WithRelationshipDefaults(r => r
                    .WithDelta(
                        2f
                    )
                    .SetUnlocked(
                        false
                    )
                    .SetUnlockType(
                        NPCRelationship.UnlockType.Recommendation
                    )
                    // She trades in Westville now, so her region, her corner of the map and the
                    // person who vouches for her all line up.
                    .WithConnectionsById(
                        new[]
                        {
                            "charles_rowland"
                        }
                    )
                )
                .WithSchedule(plan =>
                {
                    // She trades in Westville, where her connection (Charles Rowland) actually is:
                    // the corner store counter is her shop for the day.
                    Building daytime = cornerStore ?? pillville ?? nightStop;

                    if (daytime == null)
                    {

                        return;
                    }

                    plan.StayInBuilding(daytime, 0, 1380, null, "Trade Hours");

                    if (nightStop != null && nightStop != daytime)
                        plan.StayInBuilding(nightStop, 1380, 60, null, "Home");

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[PillVille] Trade-hours schedule assigned (Westville).");
                });

            global::CustomNPCExample.Utils.WvcLog.Msg("[PillVille] ConfigurePrefab completed.");
        }

        protected override void OnCreated()
        {
            try
            {
                base.OnCreated();

                Instance = this;
                Aggressiveness = 0.02f;
                Region = Region.Westville;

                Appearance.Build();
                Schedule.Enable();

                // Natives deliveries off: she does not ship, the player walks in and meets her.
                // Same shape as Roscoe's setup, and it keeps the status machine in Idle so the
                // bulk meetup request (F5) is accepted.
                try
                {
                    var supplier = gameObject?.GetComponent<Il2CppScheduleOne.Economy.Supplier>()
                        ?? gameObject?.GetComponentInChildren<Il2CppScheduleOne.Economy.Supplier>(true);

                    if (supplier != null)
                    {
                        supplier.DeliveriesEnabled = false;

                        // Her order window is left exactly as the game and her own supplier data
                        // have it. The phone's unit cap (ten) comes from the base game, and writing
                        // a bigger window here only made the label disagree with what the game
                        // actually allows.
                        var data = supplier.SupplierData;

                    }
                }
                catch (Exception)
                {

                }

                Relationship.OnUnlocked -= HandleUnlocked;
                Relationship.OnUnlocked += HandleUnlocked;

                if (Relationship.IsUnlocked)
                    UnlockSupplierSilently();

                if (!_dialogueSetupStarted)
                {
                    _dialogueSetupStarted = true;
                    MelonCoroutines.Start(ConfigureDialogueAfterLoad());
                }

                if (!_deadDropSetupStarted)
                {
                    _deadDropSetupStarted = true;
                    MelonCoroutines.Start(ConfigureExistingDeadDropAfterLoad());
                }

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[PillVille] Dr. Eleanor Scrivens loaded as the Pillville Xanax supplier."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[PillVille] OnCreated failed: " + ex);
            }
        }

        protected override void OnDestroyed()
        {
            Relationship.OnUnlocked -= HandleUnlocked;

            if (ReferenceEquals(Instance, this))
                Instance = null;

            base.OnDestroyed();
        }

        private void HandleUnlocked(
            NPCRelationship.UnlockType _,
            bool notify)
        {
            if (notify)
            {
                SendTextMessage(
                    "Eleanor Scrivens, Pillville. I put a little something aside from " +
                    "the pharmacy orders. Check your phone - you're on my list."
                );
            }

            UnlockSupplierSilently();
        }

        private void UnlockSupplierSilently()
        {
            try
            {
                if (Supplier != null && Supplier.IsSupplier)
                {
                    Supplier.Unlock();
                    global::CustomNPCExample.Utils.WvcLog.Msg("[PillVille] Supplier unlocked and products visible.");
                }
            }
            catch (Exception)
            {

            }
        }

        private IEnumerator ConfigureDialogueAfterLoad()
        {
            for (int i = 0; i < 80; i++)
            {
                try
                {
                    if (Dialogue != null && PillVilleDialogue.TryRegisterAndArm(Dialogue))
                    {
                        global::CustomNPCExample.Utils.WvcLog.Msg("[PillVille] Xanax intro dialogue armed.");
                        yield break;
                    }
                }
                catch (Exception)
                {

                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private IEnumerator ConfigureExistingDeadDropAfterLoad()
        {
            for (int i = 0; i < 80; i++)
            {
                if (TryConfigureExistingDeadDrop())
                    yield break;

                yield return new WaitForSeconds(0.25f);
            }
        }

        private static bool TryConfigureExistingDeadDrop()
        {
            try
            {
                DeadDropInstance selectedDrop =
                    DeadDropManager.Get<BehindMedicalPractice>();

                if (selectedDrop == null)
                    return false;

                string guid = selectedDrop.GUID;
                if (string.IsNullOrWhiteSpace(guid))
                    return false;

                var nativeDrops = DeadDrop.DeadDrops;
                if (nativeDrops == null || nativeDrops.Count == 0)
                    return false;

                for (int i = 0; i < nativeDrops.Count; i++)
                {
                    DeadDrop drop = nativeDrops[i];
                    if (drop == null)
                        continue;

                    bool match =
                        string.Equals(drop.GUID.ToString(), guid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(drop.BakedGUID, guid, StringComparison.OrdinalIgnoreCase);

                    if (!match)
                        continue;

                    drop.DeadDropName = PaymentDropName;
                    drop.DeadDropDescription = PaymentDropDescription;

                    global::CustomNPCExample.Utils.WvcLog.Msg("[PillVille] Dead drop claimed: " + guid);
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
