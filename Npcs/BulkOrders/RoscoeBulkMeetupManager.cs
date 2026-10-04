using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.Economy;
using MelonLoader;
using UnityEngine;

using CustomNPCExample.Quests;
using NativeSupplier = Il2CppScheduleOne.Economy.Supplier;

namespace CustomNPCExample.NPCs
{
    public static class RoscoeBulkMeetupManager
    {

        /// <summary>
        /// Ceiling the phone puts on one dead drop order: forty powder and the press to press it
        /// with. See the sibling bulk managers for why the base game's own number is not used.
        /// </summary>
        public const float RoscoeDeadDropItemLimit = 2000f;
        public const float BulkPurchaseMinimum = 500f;

        public static KeyCode RequestMeetupKey = KeyCode.F7;

        private const string BulkIntroTextSentKey =
            "WVC_Roscoe_BulkMeetup_TextSent_v1";

        private const float MeetingEndGraceSeconds = 3f;

        private static bool _meetingActive;
        private static float _meetingEndGrace;

        private static HarmonyLib.Harmony _harmony;
        private static bool _patched;

        private static NativeSupplier _roscoeSupplier;

        private static bool _requestPending;
        private static bool _introTextRoutineRunning;
        private static bool _dialogueSuppressedForMeeting;

        private static int _completedBulkMeetups;
        private static float _lifetimeBulkSpend;
        private static float _bindTimer;

        private static bool _shopCosmeticsApplied;

        private static Sprite _roscoePortrait;

        private static Sprite _customRoscoePortrait;

        public static int CompletedBulkMeetups =>
            _completedBulkMeetups;

        public static float LifetimeBulkSpend =>
            _lifetimeBulkSpend;

        public static void ApplyPatch()
        {
            if (_patched)
                return;

            try
            {
                _harmony =
                    new HarmonyLib.Harmony(
                        "westvilleconnection.roscoe.bulkmeetups"
                    );

                MethodInfo deadDropLimit =
                    AccessTools.Method(
                        typeof(NativeSupplier),
                        "GetDeadDropLimit",
                        Type.EmptyTypes
                    );

                if (deadDropLimit != null)
                {
                    _harmony.Patch(
                        deadDropLimit,
                        postfix: new HarmonyMethod(
                            typeof(RoscoeBulkMeetupManager),
                            nameof(GetDeadDropLimitPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Patched Supplier.GetDeadDropLimit."
                    );
                }

                MethodInfo orderCompleted =
                    AccessTools.Method(
                        typeof(NativeSupplier),
                        "MeetupOrderCompleted",
                        new[]
                        {
                            typeof(float)
                        }
                    );

                if (orderCompleted != null)
                {
                    _harmony.Patch(
                        orderCompleted,
                        postfix: new HarmonyMethod(
                            typeof(RoscoeBulkMeetupManager),
                            nameof(MeetupOrderCompletedPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Patched Supplier.MeetupOrderCompleted."
                    );
                }

                MethodInfo isMeetupValid =
                    AccessTools.Method(
                        typeof(NativeSupplier),
                        "IsMeetupValid"
                    );

                if (isMeetupValid != null)
                {
                    _harmony.Patch(
                        isMeetupValid,
                        postfix: new HarmonyMethod(
                            typeof(RoscoeBulkMeetupManager),
                            nameof(IsMeetupValidPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Patched Supplier.IsMeetupValid."
                    );
                }

                MethodInfo getLocation =
                    AccessTools.Method(
                        typeof(NativeSupplier),
                        "GetAppropriateLocation"
                    );

                if (getLocation != null)
                {
                    _harmony.Patch(
                        getLocation,
                        postfix: new HarmonyMethod(
                            typeof(RoscoeBulkMeetupManager),
                            nameof(GetAppropriateLocationPostfix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Patched Supplier.GetAppropriateLocation."
                    );
                }

                MethodInfo enableDeliveries =
                    AccessTools.Method(
                        typeof(NativeSupplier),
                        "EnableDeliveries"
                    );

                if (enableDeliveries != null)
                {
                    _harmony.Patch(
                        enableDeliveries,
                        prefix: new HarmonyMethod(
                            typeof(RoscoeBulkMeetupManager),
                            nameof(EnableDeliveriesPrefix)
                        )
                    );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Patched Supplier.EnableDeliveries to block Roscoe deliveries."
                    );
                }

                _patched = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Bulk] Roscoe bulk-meetup patches applied."
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Bulk] Patch setup failed: " +
                    ex
                );
            }
        }

        public static void Update()
        {
            if (Input.GetKeyDown(RequestMeetupKey))
            {
                RequestBulkMeetup();
                return;
            }

            if (!IsRegistered())
            {
                _bindTimer += Time.deltaTime;

                if (_bindTimer >= 2f)
                {
                    _bindTimer = 0f;

                    RoscoeBellweather roscoe =
                        RoscoeBellweather.Instance;

                    if (roscoe != null)
                        TryRegister(roscoe);
                }

                return;
            }

            UpdateMeetingState();
        }

        public static void ResetRuntime()
        {
            _roscoeSupplier = null;

            _requestPending = false;
            _introTextRoutineRunning = false;
            _dialogueSuppressedForMeeting = false;

            _meetingActive = false;
            _meetingEndGrace = 0f;

            _bindTimer = 0f;

            _completedBulkMeetups = 0;
            _lifetimeBulkSpend = 0f;

            _shopCosmeticsApplied = false;
        }

        private static void ApplyRoscoeShopDescription(
    NativeSupplier supplier)
        {
            if (supplier == null)
                return;

            try
            {
                object shop =
                    GetMemberValue(supplier, "Shop");

                if (shop == null)
                    return;

                SetMemberValue(
                    shop,
                    "ShopDescription",
                    "MDMA supplies"
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Bulk] Roscoe shop description set to MDMA supplies."
                );
            }
            catch (Exception)
            {

            }
        }

        private static void UpdateMeetingState()
        {
            if (!IsRegistered())
                return;

            bool meeting;

            try
            {
                meeting =
                    _roscoeSupplier.Status ==
                    NativeSupplier.ESupplierStatus.Meeting;
            }
            catch
            {
                return;
            }

            if (meeting)
            {
                _meetingEndGrace = 0f;

                if (!_meetingActive)
                {
                    _meetingActive = true;

                    try
                    {
                        RoscoeBellweather
                            .SuspendIdleScheduleForBulkMeeting();
                    }
                    catch (Exception)
                    {

                    }

                    try
                    {
                        RoscoeDialogue.SuppressForMeeting();
                    }
                    catch (Exception)
                    {

                    }

                    try
                    {
                        TeleportRoscoeToMeetupLocation(_roscoeSupplier);
                    }
                    catch (Exception)
                    {

                    }

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Roscoe entered native meeting. " +
                        "Idle schedule suspended and warped to meetup location."
                    );
                }

                return;
            }

            if (!_meetingActive)
                return;

            _meetingEndGrace += Time.deltaTime;

            if (_meetingEndGrace < MeetingEndGraceSeconds)
                return;

            _meetingActive = false;
            _meetingEndGrace = 0f;

            try
            {
                RoscoeBellweather
                    .ResumeIdleScheduleAfterBulkMeeting();
            }
            catch (Exception)
            {

            }

            try
            {
                RoscoeDialogue.RestoreAfterMeeting();
            }
            catch (Exception)
            {

            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Bulk] Roscoe meeting ended. " +
                "Idle schedule resumed."
            );
        }

        public static bool TryRegister(
            RoscoeBellweather roscoe)
        {
            if (roscoe == null ||
                roscoe.gameObject == null)
            {
                return false;
            }

            try
            {
                NativeSupplier supplier =
                    roscoe.gameObject
                        .GetComponent<NativeSupplier>();

                if (supplier == null)
                {
                    supplier =
                        roscoe.gameObject
                            .GetComponentInChildren<NativeSupplier>(
                                true
                            );
                }

                if (supplier == null &&
                    roscoe.gameObject.transform.root != null)
                {
                    supplier =
                        roscoe.gameObject
                            .transform
                            .root
                            .gameObject
                            .GetComponentInChildren<NativeSupplier>(
                                true
                            );
                }

                if (supplier == null)
                    return false;

                bool changed =
                    _roscoeSupplier == null ||
                    _roscoeSupplier.GetInstanceID() !=
                    supplier.GetInstanceID();

                _roscoeSupplier = supplier;

                if (changed)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Bound native Supplier to " +
                        "Roscoe Bellweather. Status=" +
                        SafeStatus(supplier)
                    );

                    ApplyRoscoeShopDescription(supplier);

                    QueueBulkIntroText(false);
                }

                if (changed)
                {
                    _shopCosmeticsApplied = false;
                }

                EnsureRoscoeDeliveriesDisabled(supplier);

                MelonCoroutines.Start(
                    SetupRoscoeShopAfterLoad(supplier)
                );

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        public static void TeleportRoscoeToMeetupLocation(NativeSupplier supplier)
        {
            if (supplier == null)
                return;

            try
            {
                object locObj = GetMemberValue(supplier, "_currentLocation")
                    ?? GetMemberValue(supplier, "CurrentLocation");

                SupplierLocation location = locObj as SupplierLocation;

                if (location == null)
                {
                    try
                    {
                        var locations = UnityEngine.Object.FindObjectsOfType<SupplierLocation>();
                        if (locations != null && locations.Length > 0)
                            location = locations[0];
                    }
                    catch { }
                }

                if (location != null && location.gameObject != null)
                {
                    Vector3 targetPos = location.transform.position;
                    Quaternion targetRot = location.transform.rotation;

                    Transform standPoint = location.transform.Find("StandPoint")
                        ?? location.transform.Find("MeetupPoint")
                        ?? location.transform.Find("SpawnPoint");

                    if (standPoint != null)
                    {
                        targetPos = standPoint.position;
                        targetRot = standPoint.rotation;
                    }

                    bool warped = RoscoeBellweather.TryWarp(targetPos, targetRot);

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Teleported Roscoe to meetup location: " +
                        location.gameObject.name + " at " + targetPos + " (warped=" + warped + ")"
                    );
                }
            }
            catch (Exception)
            {

            }
        }

        private static IEnumerator SetupRoscoeShopAfterLoad(
            NativeSupplier supplier)
        {
            yield return new WaitForSeconds(3f);

            if (supplier == null)
                yield break;

            ApplyRoscoeShopCosmetics(supplier);
            EnsureRoscoeDeliveriesDisabled(supplier);
        }

        private static void EnsureRoscoeDeliveriesDisabled(
    NativeSupplier supplier)
        {
            if (supplier == null)
                return;

            try
            {
                supplier.DeliveriesEnabled = false;
            }
            catch
            {
            }

            try
            {
                FieldInfo backing =
                    supplier.GetType().GetField(
                        "<DeliveriesEnabled>k__BackingField",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (backing != null)
                {
                    backing.SetValue(supplier, false);
                }
            }
            catch
            {
            }
        }

        private static void ApplyRoscoeShopCosmetics(
            NativeSupplier supplier)
        {
            if (supplier == null || _shopCosmeticsApplied)
                return;

            try
            {
                object shop =
                    GetMemberValue(supplier, "Shop");

                if (shop == null)
                    return;

                SetMemberValue(shop, "ShopCategory", "MDMA supplies");
                SetMemberValue(shop, "Category", "MDMA supplies");
                SetMemberValue(shop, "shopCategory", "MDMA supplies");

                if (_roscoePortrait == null)
                    _roscoePortrait = CreateRoscoePortrait();

                if (_roscoePortrait != null)
                {
                    int replaced =
                        ReplaceSpriteMembers(
                            shop,
                            _roscoePortrait
                        );

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Roscoe shop cosmetics applied. " +
                        "SpritesReplaced=" +
                        replaced
                    );
                }

                _shopCosmeticsApplied = true;
            }
            catch (Exception)
            {

            }
        }

        private static int ReplaceSpriteMembers(
            object target,
            Sprite sprite)
        {
            if (target == null || sprite == null)
                return 0;

            int replaced = 0;
            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    FieldInfo[] fields =
                        type.GetFields(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].FieldType != typeof(Sprite))
                            continue;

                        try
                        {
                            fields[i].SetValue(target, sprite);
                            replaced++;
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    PropertyInfo[] properties =
                        type.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    for (int i = 0; i < properties.Length; i++)
                    {
                        PropertyInfo property = properties[i];

                        if (property.PropertyType != typeof(Sprite) ||
                            !property.CanWrite ||
                            property.GetIndexParameters().Length != 0)
                        {
                            continue;
                        }

                        try
                        {
                            property.SetValue(target, sprite);
                            replaced++;
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                type = type.BaseType;
            }

            return replaced;
        }

        private static Sprite CreateRoscoePortrait()
        {
            const int size = 96;

            try
            {
                Texture2D texture =
                    new Texture2D(
                        size,
                        size,
                        TextureFormat.RGBA32,
                        false
                    );

                texture.name = "WVC_Roscoe_Portrait_Texture";
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        texture.SetPixel(
                            x,
                            y,
                            new Color(0f, 0f, 0f, 0f)
                        );
                    }
                }

                Circle(texture, 48, 48, 46, new Color(0.10f, 0.11f, 0.13f, 1f));

                RoundedRect(texture, 22, 14, 52, 34, 10,
                    new Color(0.20f, 0.31f, 0.46f, 1f));

                Rect(texture, 42, 40, 12, 12,
                    new Color(0.74f, 0.55f, 0.44f, 1f));

                Circle(texture, 48, 58, 24,
                    new Color(0.78f, 0.60f, 0.49f, 1f));

                Circle(texture, 29, 72, 11,
                    new Color(0.62f, 0.61f, 0.58f, 1f));

                Circle(texture, 67, 72, 11,
                    new Color(0.62f, 0.61f, 0.58f, 1f));

                RoundedRect(texture, 25, 71, 46, 18, 9,
                    new Color(0.55f, 0.18f, 0.12f, 1f));

                Rect(texture, 23, 70, 50, 5,
                    new Color(0.34f, 0.10f, 0.07f, 1f));

                Circle(texture, 39, 60, 3, Color.white);
                Circle(texture, 57, 60, 3, Color.white);
                Circle(texture, 39, 60, 1, new Color(0.10f, 0.10f, 0.10f, 1f));
                Circle(texture, 57, 60, 1, new Color(0.10f, 0.10f, 0.10f, 1f));

                Rect(texture, 34, 66, 10, 2, new Color(0.22f, 0.17f, 0.13f, 1f));
                Rect(texture, 52, 66, 10, 2, new Color(0.22f, 0.17f, 0.13f, 1f));

                Rect(texture, 47, 53, 3, 6, new Color(0.63f, 0.44f, 0.35f, 1f));
                Rect(texture, 42, 47, 13, 2, new Color(0.36f, 0.18f, 0.16f, 1f));

                Rect(texture, 28, 21, 8, 20, new Color(0.10f, 0.16f, 0.26f, 1f));
                Rect(texture, 60, 21, 8, 20, new Color(0.10f, 0.16f, 0.26f, 1f));

                texture.Apply();

                Sprite sprite =
                    Sprite.Create(
                        texture,
                        new Rect(0f, 0f, size, size),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );

                sprite.name = "WVC_Roscoe_Portrait";
                sprite.hideFlags = HideFlags.HideAndDontSave;

                return sprite;
            }
            catch (Exception)
            {


                return null;
            }
        }

        private static void Rect(
            Texture2D texture,
            int left,
            int bottom,
            int width,
            int height,
            Color color)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetPixel(texture, left + x, bottom + y, color);
                }
            }
        }

        private static void RoundedRect(
            Texture2D texture,
            int left,
            int bottom,
            int width,
            int height,
            int radius,
            Color color)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx =
                        Mathf.Clamp(x, radius, width - radius - 1f);

                    float cy =
                        Mathf.Clamp(y, radius, height - radius - 1f);

                    float dx = x - cx;
                    float dy = y - cy;

                    if (dx * dx + dy * dy > radius * radius)
                        continue;

                    SetPixel(texture, left + x, bottom + y, color);
                }
            }
        }

        private static void Circle(
            Texture2D texture,
            int centerX,
            int centerY,
            int radius,
            Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y > radius * radius)
                        continue;

                    SetPixel(texture, centerX + x, centerY + y, color);
                }
            }
        }

        private static void SetPixel(
            Texture2D texture,
            int x,
            int y,
            Color color)
        {
            if (texture == null ||
                x < 0 ||
                y < 0 ||
                x >= texture.width ||
                y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static bool IsRegistered()
        {
            try
            {
                return _roscoeSupplier != null &&
                       _roscoeSupplier.gameObject != null;
            }
            catch
            {
                _roscoeSupplier = null;
                return false;
            }
        }

        public static bool IsRoscoeInNativeMeeting()
        {
            try
            {
                return _roscoeSupplier != null &&
                       _roscoeSupplier.Status ==
                       NativeSupplier.ESupplierStatus.Meeting;
            }
            catch
            {
                return false;
            }
        }

        public static bool RequestBulkMeetup()
        {
            if (!IsRegistered())
            {
                if (!TryRegister(
                        RoscoeBellweather.Instance))
                {


                    return false;
                }
            }

            if (_requestPending)
            {


                return false;
            }

            NativeSupplier supplier =
                _roscoeSupplier;

            if (supplier == null)
                return false;

            try
            {
                if (supplier.Status ==
                    NativeSupplier.ESupplierStatus.Meeting)
                {
                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Roscoe is already at a meetup."
                    );

                    return true;
                }

                if (supplier.Status !=
                    NativeSupplier.ESupplierStatus.Idle)
                {


                    return false;
                }

                supplier.MeetupRequested();

                try
                {
                    RoscoeBellweather.SuspendIdleScheduleForBulkMeeting();
                }
                catch (Exception)
                {

                }

                _requestPending = true;

                MelonCoroutines.Start(
                    VerifyMeetupRequest(supplier)
                );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Bulk] Native meetup request sent to Roscoe."
                );

                return true;
            }
            catch (Exception ex)
            {
                _requestPending = false;

                MelonLogger.Error(
                    "[WVC Bulk] Meetup request failed: " +
                    ex.Message
                );

                return false;
            }
        }

        private static IEnumerator VerifyMeetupRequest(
            NativeSupplier supplier)
        {
            float deadline =
                Time.realtimeSinceStartup + 8f;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (supplier == null)
                {
                    _requestPending = false;
                    yield break;
                }

                bool meeting = false;
                bool prepping = false;

                try
                {
                    meeting =
                        supplier.Status ==
                        NativeSupplier.ESupplierStatus.Meeting;

                    prepping =
                        supplier.Status ==
                        NativeSupplier.ESupplierStatus
                            .PreppingDeadDrop;
                }
                catch
                {
                    _requestPending = false;
                    yield break;
                }

                if (meeting)
                {
                    _requestPending = false;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Roscoe accepted the meetup. " +
                        "Meet him at the marked location."
                    );

                    yield break;
                }

                if (prepping)
                {
                    _requestPending = false;



                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            _requestPending = false;


        }

        public static bool ForceUnlockBulkMeetupsAndText()
        {
            if (!IsRegistered())
            {
                if (!TryRegister(
                        RoscoeBellweather.Instance))
                {


                    return false;
                }
            }

            try
            {
                NativeSupplier.MeetupRelationshipRequirement = 0f;
                NativeSupplier.MeetupCooldown = 0;
            }
            catch { }

            QueueBulkIntroText(true);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Bulk] DEBUG: Roscoe bulk meetups unlocked."
            );

            return true;
        }

        private static void QueueBulkIntroText(
            bool force)
        {
            if (!force)
            {
                try
                {
                    if (PlayerPrefs.GetInt(
                            BulkIntroTextSentKey,
                            0
                        ) == 1)
                    {
                        return;
                    }
                }
                catch { }
            }

            if (_introTextRoutineRunning && !force)
                return;

            MelonCoroutines.Start(
                SendBulkIntroTextRoutine()
            );
        }

        private static IEnumerator SendBulkIntroTextRoutine()
        {
            _introTextRoutineRunning = true;

            for (int attempt = 0;
                 attempt < 30;
                 attempt++)
            {
                if (TrySendBulkIntroText())
                {
                    try
                    {
                        PlayerPrefs.SetInt(
                            BulkIntroTextSentKey,
                            1
                        );

                        PlayerPrefs.Save();
                    }
                    catch { }

                    _introTextRoutineRunning = false;

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Roscoe bulk-meetup intro text sent."
                    );

                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
            }

            _introTextRoutineRunning = false;


        }

        private static bool TrySendBulkIntroText()
        {
            try
            {
                return SnitchQuestManager.TryDeepPhoneDelivery(
                    "Roscoe Bellweather",
                    "It's Roscoe. Small stuff can still go through " +
                    "the drop, but bigger PMK and safrole runs are " +
                    "too hot to leave sitting around. If you need " +
                    "bulk, text me 'We need to meet up'. Bring cash."
                );
            }
            catch
            {
                return false;
            }
        }

        private static bool EnableDeliveriesPrefix(
            NativeSupplier __instance)
        {
            if (IsRoscoe(__instance))
            {
                try
                {
                    __instance.DeliveriesEnabled = false;
                }
                catch { }

                return false;
            }

            return true;
        }

        private static void IsMeetupValidPostfix(
            NativeSupplier __instance,
            ref bool __result)
        {
            if (!IsRoscoe(__instance))
                return;

            try
            {
                if (__instance.Status ==
                    NativeSupplier.ESupplierStatus.Idle)
                {
                    __result = true;
                }
            }
            catch { }
        }

        private static void GetAppropriateLocationPostfix(
            NativeSupplier __instance,
            ref SupplierLocation __result)
        {
            if (!IsRoscoe(__instance))
                return;

            if (__result != null)
                return;

            try
            {
                SupplierLocation[] locations =
                    UnityEngine.Object
                        .FindObjectsOfType<SupplierLocation>();

                if (locations != null &&
                    locations.Length > 0)
                {
                    __result = locations[0];

                    global::CustomNPCExample.Utils.WvcLog.Msg(
                        "[WVC Bulk] Assigned fallback meetup " +
                        "location to Roscoe."
                    );
                }
            }
            catch { }
        }

        private static void GetDeadDropLimitPostfix(
            NativeSupplier __instance,
            ref float __result)
        {
            if (!IsRoscoe(__instance))
                return;

            if (__result <= 0f)
                return;

            __result =
                Mathf.Min(
                    __result,
                    RoscoeDeadDropItemLimit
                );
        }

        private static void MeetupOrderCompletedPostfix(
            NativeSupplier __instance,
            float __0)
        {
            if (!IsRoscoe(__instance))
                return;

            float amount =
                Mathf.Max(0f, __0);

            if (amount < BulkPurchaseMinimum)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Bulk] Meetup purchase $" +
                    amount.ToString("0.00") +
                    " is below the bulk minimum."
                );

                return;
            }

            _completedBulkMeetups++;
            _lifetimeBulkSpend += amount;

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Bulk] Bulk purchase complete. Spend=$" +
                amount.ToString("0.00") +
                " | BulkMeetups=" +
                _completedBulkMeetups +
                " | Lifetime=$" +
                _lifetimeBulkSpend.ToString("0.00")
            );
        }

        private static Sprite CreateProceduralRoscoePortrait()
        {
            const int size = 96;

            try
            {
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "WVC_Roscoe_ShopPortrait_Texture";
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        texture.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    }
                }

                DrawCircleOnTexture(texture, 48, 48, 46, new Color(0.45f, 0.08f, 0.12f, 1f));
                DrawCircleOnTexture(texture, 48, 48, 42, new Color(0.12f, 0.13f, 0.16f, 1f));

                DrawRoundedRectOnTexture(texture, 22, 15, 52, 34, 10, new Color(0.18f, 0.32f, 0.50f, 1f));

                FillRectOnTexture(texture, 42, 40, 12, 12, new Color(0.76f, 0.57f, 0.46f, 1f));

                DrawCircleOnTexture(texture, 48, 58, 24, new Color(0.78f, 0.60f, 0.49f, 1f));

                DrawCircleOnTexture(texture, 25, 58, 5, new Color(0.68f, 0.48f, 0.38f, 1f));
                DrawCircleOnTexture(texture, 71, 58, 5, new Color(0.68f, 0.48f, 0.38f, 1f));

                DrawCircleOnTexture(texture, 29, 73, 11, new Color(0.62f, 0.61f, 0.58f, 1f));
                DrawCircleOnTexture(texture, 67, 73, 11, new Color(0.62f, 0.61f, 0.58f, 1f));

                DrawRoundedRectOnTexture(texture, 25, 72, 46, 18, 9, new Color(0.55f, 0.12f, 0.09f, 1f));
                FillRectOnTexture(texture, 23, 71, 50, 5, new Color(0.34f, 0.07f, 0.05f, 1f));

                DrawCircleOnTexture(texture, 39, 60, 3, Color.white);
                DrawCircleOnTexture(texture, 57, 60, 3, Color.white);
                DrawCircleOnTexture(texture, 39, 60, 1, new Color(0.12f, 0.12f, 0.12f, 1f));
                DrawCircleOnTexture(texture, 57, 60, 1, new Color(0.12f, 0.12f, 0.12f, 1f));

                FillRectOnTexture(texture, 34, 66, 10, 2, new Color(0.20f, 0.15f, 0.12f, 1f));
                FillRectOnTexture(texture, 52, 66, 10, 2, new Color(0.20f, 0.15f, 0.12f, 1f));

                FillRectOnTexture(texture, 47, 53, 3, 6, new Color(0.63f, 0.43f, 0.34f, 1f));

                FillRectOnTexture(texture, 42, 47, 13, 2, new Color(0.35f, 0.16f, 0.15f, 1f));

                FillRectOnTexture(texture, 28, 22, 8, 20, new Color(0.08f, 0.15f, 0.25f, 1f));
                FillRectOnTexture(texture, 60, 22, 8, 20, new Color(0.08f, 0.15f, 0.25f, 1f));

                texture.Apply();

                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = "WVC_Roscoe_ShopPortrait";
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
            catch (Exception)
            {

                return null;
            }
        }

        private static void FillRectOnTexture(Texture2D t, int x, int y, int w, int h, Color color)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    SetPixelOnTexture(t, x + dx, y + dy, color);
                }
            }
        }

        private static void DrawRoundedRectOnTexture(Texture2D t, int x, int y, int w, int h, int r, Color color)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    float cx = Mathf.Clamp(dx, r, w - r - 1f);
                    float cy = Mathf.Clamp(dy, r, h - r - 1f);
                    float distanceSqr = (dx - cx) * (dx - cx) + (dy - cy) * (dy - cy);
                    if (distanceSqr > r * r) continue;

                    SetPixelOnTexture(t, x + dx, y + dy, color);
                }
            }
        }

        private static void DrawCircleOnTexture(Texture2D t, int cx, int cy, int r, Color color)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    SetPixelOnTexture(t, cx + dx, cy + dy, color);
                }
            }
        }

        private static void SetPixelOnTexture(Texture2D t, int x, int y, Color color)
        {
            if (t == null || x < 0 || y < 0 || x >= t.width || y >= t.height) return;
            t.SetPixel(x, y, color);
        }

        public static void DumpRoscoeState()
        {
            NativeSupplier supplier =
                _roscoeSupplier ?? FindRoscoeSupplier();

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "========== WVC ROSCOE SUPPLIER DUMP =========="
            );

            if (supplier == null)
            {


                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "============================================="
                );

                return;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Status=" +
                SafeStatus(supplier)
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "DeliveriesEnabled=" +
                SafeMember(supplier, "DeliveriesEnabled") +
                " | Debt=" +
                SafeMember(supplier, "Debt")
            );

            object shop =
                GetMemberValue(supplier, "Shop");

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Shop=" +
                (shop != null
                    ? shop.GetType().Name
                    : "null")
            );

            object stash =
                GetMemberValue(supplier, "Stash");

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Stash=" +
                (stash != null
                    ? stash.GetType().Name
                    : "null")
            );

            object meetingAction =
                GetMemberValue(supplier, "_meetingAction");

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "MeetingAction=" +
                (meetingAction != null
                    ? meetingAction.GetType().Name
                    : "null")
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "CurrentLocation=" +
                SafeLocationName(supplier)
            );

            try
            {
                float distance =
                    Vector3.Distance(
                        supplier.transform.position,
                        RoscoeBellweather.HomePosition
                    );

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "DistanceFromHome=" +
                    distance.ToString("0.00")
                );
            }
            catch { }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "MeetingActive=" +
                _meetingActive +
                " | DialogueSuppressed=" +
                _dialogueSuppressedForMeeting +
                " | SampleClaimed=" +
                RoscoeDialogue.SampleClaimed
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "BulkMeetups=" +
                _completedBulkMeetups +
                " | LifetimeSpend=$" +
                _lifetimeBulkSpend.ToString("0.00")
            );

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "============================================="
            );
        }

        private static NativeSupplier FindRoscoeSupplier()
        {
            try
            {
                RoscoeBellweather roscoe =
                    RoscoeBellweather.Instance;

                if (roscoe == null ||
                    roscoe.gameObject == null)
                {
                    return null;
                }

                NativeSupplier supplier =
                    roscoe.gameObject
                        .GetComponent<NativeSupplier>();

                if (supplier != null)
                    return supplier;

                supplier =
                    roscoe.gameObject
                        .GetComponentInChildren<NativeSupplier>(
                            true
                        );

                if (supplier != null)
                    return supplier;

                Transform root =
                    roscoe.gameObject.transform.root;

                if (root != null)
                {
                    supplier =
                        root.gameObject
                            .GetComponentInChildren<NativeSupplier>(
                                true
                            );
                }

                return supplier;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsRoscoe(
            NativeSupplier supplier)
        {
            if (supplier == null)
                return false;

            try
            {
                if (_roscoeSupplier != null &&
                    supplier.GetInstanceID() ==
                    _roscoeSupplier.GetInstanceID())
                {
                    return true;
                }
            }
            catch { }

            try
            {
                RoscoeBellweather roscoe =
                    RoscoeBellweather.Instance;

                if (roscoe != null &&
                    roscoe.gameObject != null &&
                    supplier.gameObject != null)
                {
                    Transform a =
                        roscoe.gameObject.transform.root;

                    Transform b =
                        supplier.gameObject.transform.root;

                    if (a != null &&
                        b != null &&
                        a.GetInstanceID() == b.GetInstanceID())
                    {
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                string name =
                    supplier.name ?? string.Empty;

                return name.IndexOf(
                    "roscoe",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeStatus(
            NativeSupplier supplier)
        {
            try
            {
                return supplier.Status.ToString();
            }
            catch
            {
                return "?";
            }
        }

        private static string SafeLocationName(
            NativeSupplier supplier)
        {
            object location =
                GetMemberValue(supplier, "_currentLocation");

            if (location == null)
                return "null";

            try
            {
                Component component =
                    location as Component;

                if (component != null)
                    return component.gameObject.name;
            }
            catch { }

            return location.GetType().Name;
        }

        private static string SafeMember(
            object target,
            string name)
        {
            object value =
                GetMemberValue(target, name);

            return value != null
                ? value.ToString()
                : "?";
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null ||
                string.IsNullOrEmpty(name))
            {
                return null;
            }

            Type type =
                target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null &&
                        property.GetIndexParameters().Length == 0)
                    {
                        return property.GetValue(target);
                    }
                }
                catch { }

                try
                {
                    FieldInfo field =
                        type.GetField(
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

        private static void SetAllSpriteMembers(object target, Sprite sprite)
        {
            if (target == null || sprite == null)
                return;

            Type type = target.GetType();
            while (type != null)
            {
                try
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        if (field.FieldType == typeof(Sprite))
                        {
                            try { field.SetValue(target, sprite); } catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < properties.Length; i++)
                    {
                        PropertyInfo property = properties[i];
                        if (property.PropertyType == typeof(Sprite) && property.CanWrite)
                        {
                            try { property.SetValue(target, null); property.SetValue(target, sprite); } catch { }
                        }
                    }
                }
                catch { }

                type = type.BaseType;
            }
        }

        public static void ProbeRoscoeShop()
        {
            NativeSupplier supplier =
                _roscoeSupplier ?? FindRoscoeSupplier();

            global::CustomNPCExample.Utils.WvcLog.Msg("========== ROSCOE SHOP PROBE ==========");

            if (supplier == null)
            {

                global::CustomNPCExample.Utils.WvcLog.Msg("=======================================");
                return;
            }

            object shop =
                GetMemberValue(supplier, "Shop");

            if (shop == null)
            {

                global::CustomNPCExample.Utils.WvcLog.Msg("=======================================");
                return;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "Shop type = " + shop.GetType().FullName
            );

            Component shopComponent =
                shop as Component;

            if (shopComponent != null &&
                shopComponent.gameObject != null)
            {
                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "Shop GameObject = " +
                    shopComponent.gameObject.name
                );

                Transform parent =
                    shopComponent.transform.parent;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "Shop parent = " +
                    (parent != null ? parent.name : "null")
                );
            }

            string[] nameFields =
            {
        "ShopName",
        "shopName",
        "Name",
        "name",
        "ShopCategory",
        "Category",
        "shopCategory",
        "SubTitle",
        "Subtitle",
        "subtitle"
    };

            for (int i = 0; i < nameFields.Length; i++)
            {
                object value =
                    GetMemberValue(shop, nameFields[i]);

                if (value == null)
                    continue;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    nameFields[i] + " = " + value
                );
            }

            Type type = shop.GetType();
            int spriteCount = 0;

            while (type != null)
            {
                try
                {
                    FieldInfo[] fields =
                        type.GetFields(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (fields[i].FieldType != typeof(Sprite))
                            continue;

                        spriteCount++;

                        object v = null;

                        try
                        {
                            v = fields[i].GetValue(shop);
                        }
                        catch { }

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "SPRITE FIELD " +
                            type.Name + "." + fields[i].Name +
                            " = " +
                            (v is Sprite s ? s.name : "null")
                        );
                    }
                }
                catch { }

                try
                {
                    PropertyInfo[] props =
                        type.GetProperties(
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    for (int i = 0; i < props.Length; i++)
                    {
                        if (props[i].PropertyType != typeof(Sprite))
                            continue;

                        if (props[i].GetIndexParameters().Length != 0)
                            continue;

                        spriteCount++;

                        object v = null;

                        try
                        {
                            v = props[i].GetValue(shop);
                        }
                        catch { }

                        global::CustomNPCExample.Utils.WvcLog.Msg(
                            "SPRITE PROP " +
                            type.Name + "." + props[i].Name +
                            " = " +
                            (v is Sprite s2 ? s2.name : "null") +
                            " writable=" + props[i].CanWrite
                        );
                    }
                }
                catch { }

                type = type.BaseType;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg("Total sprite members = " + spriteCount);
            global::CustomNPCExample.Utils.WvcLog.Msg("=======================================");
        }

        private static bool SetMemberValue(
            object target,
            string name,
            object value)
        {
            if (target == null || string.IsNullOrEmpty(name))
                return false;

            Type type = target.GetType();

            while (type != null)
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null &&
                        property.CanWrite &&
                        property.GetIndexParameters().Length == 0)
                    {
                        property.SetValue(target, value);
                        return true;
                    }
                }
                catch
                {
                }

                try
                {
                    FieldInfo field =
                        type.GetField(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (field != null)
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                }
                catch
                {
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
