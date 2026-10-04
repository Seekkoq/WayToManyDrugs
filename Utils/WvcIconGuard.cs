using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    public static class WvcIconGuard
    {
        private const float CaptureRadius = 80f;

        private const int DiagnosticCaptures = 10;

        private static readonly List<Renderer> HiddenRenderers =
            new List<Renderer>();

        private static readonly HashSet<string> DiagnosticRoots =
            new HashSet<string>();

        private static readonly List<Transform> RigRoots = new List<Transform>();

        private static int _guardDepth;
        private static int _captureCount;

        private static bool _restorePending;
        private static float _restoreGraceTimer = 0.5f;
        private static bool _positionsLogged;

        private static bool _patched;
        private static bool _patchAttempted;
        private static float _patchRetryTimer;

        private static float _parkTimer = 5f;
        private static bool _parkLogged;

        private static Il2CppScheduleOne.DevUtilities.IconGenerator _generator;

        public static void Update()
        {
            ParkStagedContainerIfIdle();

            if (_restorePending)
            {
                _restoreGraceTimer -= Time.deltaTime;

                if (_restoreGraceTimer <= 0f)
                    DoRestore();
            }

            if (_patched)
                return;

            if (!_patchAttempted)
            {
                TryPatch();
                return;
            }

            _patchRetryTimer += Time.deltaTime;

            if (_patchRetryTimer < 2f)
                return;

            _patchRetryTimer = 0f;
            TryPatch();
        }

        /// <summary>
        /// Keeps S1API's staged NPC prefab container out of the world.
        ///
        /// The container holds the prefab copies S1API stages for icon rendering. It is parked far
        /// away during a capture, but a fresh one can be built between captures and then it sits in
        /// the scene looking like a pile of NPC faces. This only ever moves it while no capture is
        /// running, so an icon being rendered cannot be yanked away mid-frame.
        /// </summary>
        private static void ParkStagedContainerIfIdle()
        {
            if (_guardDepth > 0 || _restorePending)
                return;

            _parkTimer -= Time.deltaTime;

            if (_parkTimer > 0f)
                return;

            _parkTimer = 5f;

            try
            {
                GameObject container = GetStagedPrefabContainer();

                if (container == null || container.transform == null)
                    return;

                Vector3 position = container.transform.position;

                if (position.sqrMagnitude > 1000000f)
                    return;

                container.transform.position = new Vector3(0f, -10000f, 0f);

                if (!_parkLogged)
                {
                    _parkLogged = true;


                }
            }
            catch
            {
            }
        }

        private static void TryPatch()
        {
            _patchAttempted = true;

            try
            {
                var harmony = new HarmonyLib.Harmony("wvc.icon.guard");

                var prefix =
                    new HarmonyLib.HarmonyMethod(typeof(WvcIconGuard), nameof(HideForCapture));
                var postfix =
                    new HarmonyLib.HarmonyMethod(typeof(WvcIconGuard), nameof(RestoreAfterCapture));

                int patchedCount = 0;

                patchedCount += PatchByNames(
                    typeof(Il2CppScheduleOne.DevUtilities.IconGenerator),
                    new[] { "GenerateIcon", "GeneratePackagingIcon", "GetTexture" },
                    harmony,
                    prefix,
                    postfix);

                Type factoryType =
                    HarmonyLib.AccessTools.TypeByName("S1API.Rendering.IconFactory");

                if (factoryType != null)
                {
                    patchedCount += PatchByNames(
                        factoryType,
                        new[]
                        {
                            "GenerateIcon",
                            "GenerateIconSprite",
                            "GeneratePackagingIcon",
                            "GeneratePackagingIconSprite"
                        },
                        harmony,
                        prefix,
                        postfix);
                }
                else
                {

                }

                if (patchedCount > 0)
                {
                    _patched = true;

                }
            }
            catch (Exception)
            {

            }
        }

        private static int PatchByNames(
            Type type,
            string[] names,
            HarmonyLib.Harmony harmony,
            HarmonyLib.HarmonyMethod prefix,
            HarmonyLib.HarmonyMethod postfix)
        {
            int count = 0;

            if (type == null)
                return 0;

            List<System.Reflection.MethodInfo> methods;

            try
            {
                methods = HarmonyLib.AccessTools.GetDeclaredMethods(type);
            }
            catch (Exception)
            {

                return 0;
            }

            foreach (System.Reflection.MethodInfo method in methods)
            {
                if (method == null || method.IsAbstract)
                    continue;

                bool wanted = false;

                foreach (string name in names)
                {
                    if (method.Name == name)
                    {
                        wanted = true;
                        break;
                    }
                }

                if (!wanted)
                    continue;

                try
                {
                    System.Reflection.ParameterInfo[] parameters =
                        method.GetParameters();

                    HarmonyLib.HarmonyMethod chosenPrefix;

                    if (parameters.Length > 0 &&
                        parameters[0].ParameterType == typeof(Transform))
                    {
                        chosenPrefix = ModelAwarePrefix;
                    }
                    else if (parameters.Length > 1 &&
                             parameters[0].ParameterType == typeof(string) &&
                             parameters[1].ParameterType == typeof(string))
                    {
                        chosenPrefix = PackagingAwarePrefix;
                    }
                    else
                    {
                        chosenPrefix = prefix;
                    }

                    harmony.Patch(method, prefix: chosenPrefix, postfix: postfix);
                    count++;
                }
                catch (Exception)
                {

                }
            }

            return count;
        }

        private static HarmonyLib.HarmonyMethod ModelAwarePrefix =>
            new HarmonyLib.HarmonyMethod(typeof(WvcIconGuard), nameof(HideForCaptureModel));

        private static HarmonyLib.HarmonyMethod PackagingAwarePrefix =>
            new HarmonyLib.HarmonyMethod(typeof(WvcIconGuard), nameof(HideForCapturePackaging));

        private static Transform _capturedModel;
        private static Transform _pendingCapturedModel;

        private static string _pendingPackagingId;
        private static string _pendingProductId;

        public static void HideForCaptureModel(Transform __0)
        {
            _pendingCapturedModel = __0;
            HideForCapture();

            // This prefix runs just before the native rig renders the model, which is the first
            // moment the rig exists and the icon clone can be turned onto its camera.
            WvcIconPose.Apply(__0, _generator);
        }

        public static void HideForCapturePackaging(string __0, string __1)
        {
            _pendingPackagingId = __0;
            _pendingProductId = __1;
            HideForCapture();
        }

        private static bool IsCapturedModel(Transform t)
        {
            if (_capturedModel == null || t == null)
                return false;

            try
            {
                return t == _capturedModel ||
                       t.IsChildOf(_capturedModel) ||
                       _capturedModel.IsChildOf(t);
            }
            catch
            {
                return false;
            }
        }

        public static void HideForCapture()
        {
            _guardDepth++;

            if (_guardDepth > 1)
                return;

            _capturedModel = _pendingCapturedModel;
            _pendingCapturedModel = null;
            _packagingId = _pendingPackagingId;
            _productId = _pendingProductId;
            _pendingPackagingId = null;
            _pendingProductId = null;

            try
            {
                if (!_restorePending)
                    HiddenRenderers.Clear();

                _restorePending = false;

                _generator = UnityEngine.Object.FindObjectOfType<
                    Il2CppScheduleOne.DevUtilities.IconGenerator>();

                if (_generator == null)
                {
                    HideFallbackTargets();
                    ReportHidden();
                    return;
                }

                CacheRigRoots();

                Vector3 cameraPosition = GetCaptureCameraPosition();

                LogPositionsOnce(cameraPosition);
                RelocateStagedContainer(cameraPosition);

                // NOTE: staged NPC prefab renderers are deliberately NOT hidden
                // here. The container is already relocated out of every camera's
                // view (above), and hiding the templates made S1API spawn clones
                // with disabled skin renderers whenever an NPC spawned during a
                // capture window - leaving NPCs with clothes but no skin.
                HideCategory(GetNpcRenderers(), "LiveNPCs");
                HideCategory(GetPlayerRenderers(), "PlayerAvatar");
                HideCategory(
                    GetNearCameraRenderers(cameraPosition),
                    "NearCamera");

                ReportHidden();
            }
            catch (Exception)
            {

            }
        }

        private static void LogPositionsOnce(Vector3 cameraPosition)
        {
            if (_positionsLogged)
                return;

            _positionsLogged = true;

            try
            {
                GameObject container = GetStagedPrefabContainer();

                Vector3 containerPos = container != null
                    ? container.transform.position
                    : Vector3.zero;

            }
            catch
            {
            }
        }

        private static void RelocateStagedContainer(Vector3 cameraPosition)
        {
            try
            {
                GameObject container = GetStagedPrefabContainer();

                if (container == null)
                    return;

                Transform containerTransform = container.transform;
                Vector3 pos = containerTransform.position;
                float distance = Vector3.Distance(cameraPosition, pos);

                if (distance >= 2000f)
                    return;

                Vector3 direction = distance > 0.01f
                    ? (pos - cameraPosition).normalized
                    : new Vector3(0f, -1f, 0f);

                Vector3 newPos = cameraPosition + direction * 10000f;

                containerTransform.position = newPos;

            }
            catch (Exception)
            {

            }
        }

        private static void HideCategory(List<Renderer> renderers, string source)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || !renderer.enabled)
                    continue;

                Transform t = renderer.transform;

                if (IsPartOfRig(t))
                    continue;

                if (IsCapturedModel(t))
                    continue;

                renderer.enabled = false;
                HiddenRenderers.Add(renderer);

                DiagnosticRoots.Add(source);
            }
        }

        private static Vector3 GetCaptureCameraPosition()
        {
            if (_generator == null)
                return Vector3.zero;

            try
            {
                Camera camera = _generator.CameraPosition;

                if (camera != null)
                    return camera.transform.position;
            }
            catch
            {
            }

            try
            {
                return _generator.transform.position;
            }
            catch
            {
                return Vector3.zero;
            }
        }

        private static void ReportHidden()
        {
            _captureCount++;

            if (HiddenRenderers.Count == 0)
            {
                if (_captureCount <= DiagnosticCaptures)
                {
                }

                return;
            }

            var names = new System.Text.StringBuilder();

            foreach (string name in DiagnosticRoots)
            {
                if (names.Length > 0)
                    names.Append(", ");

                names.Append(name);
            }

            string subject =
                string.IsNullOrEmpty(_productId)
                    ? ""
                    : " [" + _productId +
                      (string.IsNullOrEmpty(_packagingId)
                          ? ""
                          : " / " + _packagingId) + "]";

        }

        private static readonly List<Renderer> StagedRenderers =
            new List<Renderer>();

        private static readonly List<Renderer> NpcCandidates =
            new List<Renderer>();

        private static readonly List<Renderer> PlayerRenderers =
            new List<Renderer>();

        private static readonly List<Renderer> NearCameraRenderers =
            new List<Renderer>();

        private static Transform _stagedRoot;
        private static string _packagingId;
        private static string _productId;
        private static float _npcRefreshTimer = -1f;
        private static float _sceneRefreshTimer = -1f;

        private static List<Renderer> GetStagedRenderers()
        {
            GameObject container = GetStagedPrefabContainer();

            if (container == null)
            {
                _stagedRoot = null;
                StagedRenderers.Clear();
                return StagedRenderers;
            }

            _stagedRoot = container.transform;

            StagedRenderers.Clear();
            AddRenderersUnder(container.transform, StagedRenderers);
            return StagedRenderers;
        }

        private static List<Renderer> GetNpcRenderers()
        {
            _npcRefreshTimer -= Time.deltaTime;

            if (_npcRefreshTimer > 0f)
                return NpcCandidates;

            _npcRefreshTimer = 2f;
            NpcCandidates.Clear();

            foreach (Il2CppScheduleOne.NPCs.NPC npc in
                UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.NPCs.NPC>())
            {
                if (npc != null && npc.gameObject != null)
                    AddRenderersUnder(npc.gameObject.transform, NpcCandidates);
            }

            return NpcCandidates;
        }

        private static List<Renderer> GetPlayerRenderers()
        {
            PlayerRenderers.Clear();

            try
            {
                Il2CppScheduleOne.PlayerScripts.Player player =
                    Il2CppScheduleOne.PlayerScripts.Player.Local;

                if (player != null && player.gameObject != null)
                    AddRenderersUnder(player.gameObject.transform, PlayerRenderers);
            }
            catch
            {
            }

            return PlayerRenderers;
        }

        private static List<Renderer> GetNearCameraRenderers(Vector3 cameraPosition)
        {
            _sceneRefreshTimer -= Time.deltaTime;

            if (_sceneRefreshTimer > 0f)
                return NearCameraRenderers;

            _sceneRefreshTimer = 5f;
            NearCameraRenderers.Clear();

            Renderer[] all = UnityEngine.Object.FindObjectsOfType<Renderer>();

            for (int i = 0; i < all.Length; i++)
            {
                Renderer renderer = all[i];

                if (renderer == null)
                    continue;

                Transform t = renderer.transform;

                float byTransform = Vector3.Distance(cameraPosition, t.position);

                if (byTransform > CaptureRadius)
                {
                    try
                    {
                        if (Vector3.Distance(
                                cameraPosition, renderer.bounds.center) >
                            CaptureRadius)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                NearCameraRenderers.Add(renderer);
            }

            return NearCameraRenderers;
        }

        private static void AddRenderersUnder(
            Transform root,
            List<Renderer> target)
        {
            if (root == null || target == null)
                return;

            foreach (Renderer renderer in
                root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && !target.Contains(renderer))
                    target.Add(renderer);
            }
        }

        private static void HideFallbackTargets()
        {
            GameObject container = GetStagedPrefabContainer();

            if (container != null)
                HideRenderersUnder(container.transform);

            foreach (Il2CppScheduleOne.NPCs.NPC npc in
                UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.NPCs.NPC>())
            {
                if (npc == null || npc.gameObject == null)
                    continue;

                HideRenderersUnder(npc.gameObject.transform);
            }
        }

        private static void HideRenderersUnder(Transform root)
        {
            foreach (Renderer renderer in
                root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    HiddenRenderers.Add(renderer);
                }
            }
        }

        private static void CacheRigRoots()
        {
            RigRoots.Clear();

            if (_generator == null)
                return;

            AddRigRoot(_generator.ItemContainer);
            AddRigRoot(_generator.MainContainer);

            try
            {
                if (_generator.gameObject != null)
                    AddRigRoot(_generator.gameObject.transform);
            }
            catch
            {
            }

            try
            {
                GameObject canvas = _generator.Canvas;

                if (canvas != null)
                    RigRoots.Add(canvas.transform);
            }
            catch
            {
            }

            try
            {
                var visuals = _generator.Visuals;

                if (visuals != null)
                {
                    for (int i = 0; i < visuals.Count; i++)
                    {
                        var packagingVisuals = visuals[i];

                        if (packagingVisuals == null)
                            continue;

                        AddRigRoot(packagingVisuals.TopLevelTransform);
                    }
                }
            }
            catch
            {
            }
        }

        private static void AddRigRoot(Transform root)
        {
            if (root != null && !RigRoots.Contains(root))
                RigRoots.Add(root);
        }

        private static bool IsPartOfRig(Transform t)
        {
            for (int i = 0; i < RigRoots.Count; i++)
            {
                Transform root = RigRoots[i];

                if (root == null)
                    continue;

                if (t == root || t.IsChildOf(root))
                    return true;
            }

            return false;
        }

        private static string RootName(Transform t)
        {
            try
            {
                Transform current = t;

                while (current.parent != null &&
                       current.parent != _generator.transform)
                {
                    current = current.parent;
                }

                return current.name;
            }
            catch
            {
                return "<unknown>";
            }
        }

        private static GameObject GetStagedPrefabContainer()
        {
            try
            {
                var containerType =
                    HarmonyLib.AccessTools.TypeByName("S1API.Entities.NPCPrefabContainer");

                if (containerType == null)
                    return null;

                var method = HarmonyLib.AccessTools.Method(
                    containerType,
                    "GetOrCreatePrefabsContainer");

                return method?.Invoke(null, null) as GameObject;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public static void RestoreAfterCapture()
        {
            _guardDepth--;

            if (_guardDepth > 0)
                return;

            _guardDepth = 0;

            _restorePending = true;
            _restoreGraceTimer = 0.5f;

            _capturedModel = null;
            _pendingCapturedModel = null;
        }

        private static void DoRestore()
        {
            _restorePending = false;

            foreach (Renderer renderer in HiddenRenderers)
            {
                try
                {
                    if (renderer != null)
                        renderer.enabled = true;
                }
                catch
                {
                }
            }

            HiddenRenderers.Clear();
            DiagnosticRoots.Clear();
        }
    }
}
