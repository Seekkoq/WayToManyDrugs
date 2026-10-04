using System;
using System.Reflection;
using MelonLoader;
using S1API.Items;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    public static class UnbakedGummyMixVisual
    {
        private static bool _applied;

        private static GameObject _model;
        private static Sprite _icon;

        private static readonly Color GelColor =
            new Color(0.84f, 0.16f, 0.14f, 1f);

        private static readonly Color GelDeep =
            new Color(0.58f, 0.07f, 0.07f, 1f);

        private static readonly Color TrayMetal =
            new Color(0.76f, 0.78f, 0.80f, 1f);

        private static readonly Color TrayEdge =
            new Color(0.52f, 0.54f, 0.56f, 1f);

        public static GameObject GetModel()
        {
            return _model;
        }

        public static bool Apply()
        {
            if (_applied)
                return true;

            try
            {
                Il2CppScheduleOne.ItemFramework.ItemDefinition definition =
                    GetRawDefinition(UnbakedGummyMix.ItemId);

                if (definition == null)
                {


                    return false;
                }

                _model = BuildModel();

                bool equippable =
                    ApplyEquippableRepresentation(
                        definition,
                        _model,
                        Vector3.one * 0.95f
                    );

                bool station =
                    ApplyStationRepresentation(
                        definition,
                        _model,
                        Vector3.one * 1.20f
                    );

                bool stored =
                    ApplyStoredRepresentation(
                        definition,
                        _model,
                        Vector3.one * 1.20f
                    );

                _icon =
                    RenderIsolatedIcon(
                        _model,
                        "WVC_UnbakedGummyMix_Icon"
                    );

                bool iconApplied =
                    ApplyIcon(definition, _icon);

                MoveSourceOffscreen(_model);

                _applied = true;

                global::CustomNPCExample.Utils.WvcLog.Msg(
                    "[WVC Gummy Mix] Visual applied. " +
                    "Equippable=" + equippable +
                    ", StationItem=" + station +
                    ", StoredItem=" + stored +
                    ", Icon=" + iconApplied
                );

                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC Gummy Mix] Visual setup failed: " + ex
                );

                return false;
            }
        }

        private static GameObject BuildModel()
        {
            GameObject root =
                new GameObject(
                    "WVC_Custom_UnbakedGummyMix_CleanSheet"
                );

            AddCube(
                root.transform,
                "TrayFloor",
                new Vector3(0.118f, 0.0035f, 0.084f),
                new Vector3(0f, 0.00175f, 0f),
                TrayMetal,
                0.55f,
                0.60f
            );

            AddCube(
                root.transform,
                "TrayLipFront",
                new Vector3(0.122f, 0.010f, 0.0045f),
                new Vector3(0f, 0.006f, -0.0435f),
                TrayEdge,
                0.50f,
                0.60f
            );

            AddCube(
                root.transform,
                "TrayLipBack",
                new Vector3(0.122f, 0.010f, 0.0045f),
                new Vector3(0f, 0.006f, 0.0435f),
                TrayEdge,
                0.50f,
                0.60f
            );

            AddCube(
                root.transform,
                "TrayLipLeft",
                new Vector3(0.0045f, 0.010f, 0.084f),
                new Vector3(-0.061f, 0.006f, 0f),
                TrayEdge,
                0.50f,
                0.60f
            );

            AddCube(
                root.transform,
                "TrayLipRight",
                new Vector3(0.0045f, 0.010f, 0.084f),
                new Vector3(0.061f, 0.006f, 0f),
                TrayEdge,
                0.50f,
                0.60f
            );

            AddCube(
                root.transform,
                "CleanGelSlab",
                new Vector3(0.100f, 0.0065f, 0.066f),
                new Vector3(0f, 0.00675f, 0f),
                GelColor,
                0.96f,
                0f
            );

            AddCube(
                root.transform,
                "GelFrontEdge",
                new Vector3(0.100f, 0.003f, 0.003f),
                new Vector3(0f, 0.006f, -0.0345f),
                GelDeep,
                0.88f,
                0f
            );

            AddCube(
                root.transform,
                "GelRightEdge",
                new Vector3(0.003f, 0.003f, 0.066f),
                new Vector3(0.0515f, 0.006f, 0f),
                GelDeep,
                0.88f,
                0f
            );

            return root;
        }

        private static Sprite RenderIsolatedIcon(
    GameObject model,
    string iconName
)
        {
            const int IconSize = 128;

            if (model == null)
                return null;

            GameObject rig =
                new GameObject(iconName + "_Rig");

            rig.transform.position =
                new Vector3(6000f, 6000f, 6000f);

            GameObject instance =
                UnityEngine.Object.Instantiate(model);

            instance.transform.SetParent(
                rig.transform,
                false
            );

            instance.transform.localPosition =
                Vector3.zero;

            instance.transform.localRotation =
                Quaternion.Euler(
                    35f,
                    -25f,
                    0f
                );

            instance.transform.localScale =
                Vector3.one;

            instance.SetActive(true);

            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>(true);

            if (renderers == null ||
                renderers.Length == 0)
            {
                UnityEngine.Object.Destroy(rig);
                return null;
            }

            Bounds bounds =
                renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            GameObject lightObject =
                new GameObject(iconName + "_Light");

            lightObject.transform.SetParent(
                rig.transform,
                false
            );

            Light light =
                lightObject.AddComponent<Light>();

            light.type =
                LightType.Directional;

            light.intensity = 1.5f;

            lightObject.transform.rotation =
                Quaternion.Euler(
                    55f,
                    -30f,
                    0f
                );

            GameObject cameraObject =
                new GameObject(iconName + "_Camera");

            cameraObject.transform.SetParent(
                rig.transform,
                false
            );

            Camera camera =
                cameraObject.AddComponent<Camera>();

            camera.clearFlags =
                CameraClearFlags.SolidColor;

            camera.backgroundColor =
                new Color(0f, 0f, 0f, 0f);

            camera.orthographic = true;

            float largestExtent =
                Mathf.Max(
                    bounds.extents.x,
                    Mathf.Max(
                        bounds.extents.y,
                        bounds.extents.z
                    )
                );

            camera.orthographicSize =
                Mathf.Max(
                    0.05f,
                    largestExtent * 1.45f
                );

            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;

            camera.transform.position =
                bounds.center +
                new Vector3(
                    0.10f,
                    0.22f,
                    -0.50f
                );

            camera.transform.LookAt(
                bounds.center
            );

            RenderTexture target =
                new RenderTexture(
                    IconSize,
                    IconSize,
                    16,
                    RenderTextureFormat.ARGB32
                );

            camera.targetTexture = target;

            camera.Render();

            RenderTexture previous =
                RenderTexture.active;

            RenderTexture.active =
                target;

            Texture2D texture =
                new Texture2D(
                    IconSize,
                    IconSize,
                    TextureFormat.RGBA32,
                    false
                );

            texture.name =
                iconName + "_Texture";

            texture.ReadPixels(
                new Rect(
                    0f,
                    0f,
                    IconSize,
                    IconSize
                ),
                0,
                0
            );

            texture.Apply();

            RenderTexture.active =
                previous;

            camera.targetTexture =
                null;

            UnityEngine.Object.Destroy(
                target
            );

            UnityEngine.Object.Destroy(
                rig
            );

            UnityEngine.Object.DontDestroyOnLoad(
                texture
            );

            Sprite sprite =
                Sprite.Create(
                    texture,
                    new Rect(
                        0f,
                        0f,
                        IconSize,
                        IconSize
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    )
                );

            sprite.name =
                iconName;

            UnityEngine.Object.DontDestroyOnLoad(
                sprite
            );

            return sprite;
        }

        private static int FindIsolationLayer()
        {
            for (int layer = 31; layer >= 8; layer--)
            {
                if (string.IsNullOrEmpty(
                        LayerMask.LayerToName(layer)))
                {
                    return layer;
                }
            }

            return 31;
        }

        private static void SetLayerRecursive(
            GameObject target,
            int layer)
        {
            if (target == null)
                return;

            target.layer = layer;

            for (int i = 0; i < target.transform.childCount; i++)
            {
                Transform child = target.transform.GetChild(i);

                if (child != null)
                    SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static bool ApplyEquippableRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                if (definition.Equippable == null ||
                    definition.Equippable.gameObject == null)
                {


                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        definition.Equippable.gameObject,
                        "Equippable",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.Equipping.Equippable component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Equipping.Equippable
                    >();

                if (component == null)
                    return false;

                definition.Equippable = component;

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static bool ApplyStationRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.StorableItemDefinition storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StationItem == null ||
                    storable.StationItem.gameObject == null)
                {


                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        storable.StationItem.gameObject,
                        "StationItem",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.StationFramework.StationItem component =
                    clone.GetComponent<
                        Il2CppScheduleOne.StationFramework.StationItem
                    >();

                if (component == null)
                    return false;

                storable.StationItem = component;

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static bool ApplyStoredRepresentation(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                Il2CppScheduleOne.ItemFramework.StorableItemDefinition storable =
                    definition.TryCast<
                        Il2CppScheduleOne.ItemFramework.StorableItemDefinition
                    >();

                if (storable == null ||
                    storable.StoredItem == null ||
                    storable.StoredItem.gameObject == null)
                {


                    return false;
                }

                GameObject clone =
                    BuildRepresentationClone(
                        storable.StoredItem.gameObject,
                        "StoredItem",
                        customModel,
                        visualScale
                    );

                if (clone == null)
                    return false;

                Il2CppScheduleOne.Storage.StoredItem component =
                    clone.GetComponent<
                        Il2CppScheduleOne.Storage.StoredItem
                    >();

                if (component == null)
                    return false;

                storable.StoredItem = component;

                return true;
            }
            catch (Exception)
            {


                return false;
            }
        }

        private static GameObject BuildRepresentationClone(
            GameObject template,
            string context,
            GameObject customModel,
            Vector3 localScale)
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone =
                UnityEngine.Object.Instantiate(template);

            clone.name =
                "WVC_" + context + "_UnbakedGummyMix";

            clone.SetActive(true);

            clone.transform.position =
                new Vector3(0f, -22000f, 0f);

            UnityEngine.Object.DontDestroyOnLoad(clone);

            Renderer[] oldRenderers =
                clone.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in oldRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            GameObject visual =
                UnityEngine.Object.Instantiate(customModel);

            visual.name =
                "WVC_CustomVisual_UnbakedGummyMix_" + context;

            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.SetActive(true);

            Renderer[] customRenderers =
                visual.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in customRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Gummy Mix] Built " +
                context +
                " representation with " +
                customRenderers.Length +
                " renderers."
            );

            return clone;
        }

        private static bool ApplyIcon(
            Il2CppScheduleOne.ItemFramework.ItemDefinition definition,
            Sprite icon)
        {
            if (icon == null)
                return false;

            TrySetMember(definition, "Icon", null);

            return TrySetMember(definition, "Icon", icon);
        }

        private static GameObject AddCube(
            Transform parent,
            string name,
            Vector3 size,
            Vector3 localPosition,
            Color color,
            float smoothness,
            float metallic)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            part.name = name;

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = size;

            ApplyMaterial(part, name, color, smoothness, metallic);
            RemovePrimitiveCollider(part);

            return part;
        }

        private static GameObject AddSphere(
            Transform parent,
            string name,
            float radius,
            Vector3 localPosition,
            Color color,
            float smoothness,
            Vector3 squash)
        {
            GameObject part =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            part.name = name;

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;

            part.transform.localScale =
                new Vector3(
                    radius * 2f * squash.x,
                    radius * 2f * squash.y,
                    radius * 2f * squash.z
                );

            ApplyMaterial(part, name, color, smoothness, 0f);
            RemovePrimitiveCollider(part);

            return part;
        }

        private static void ApplyMaterial(
            GameObject part,
            string name,
            Color color,
            float smoothness,
            float metallic)
        {
            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer == null)
                return;

            renderer.sharedMaterial =
                CreateMaterial(
                    name + "_Material",
                    color,
                    smoothness,
                    metallic
                );
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float smoothness,
            float metallic)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No compatible shader found."
                );
            }

            Material material = new Material(shader);
            material.name = name;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            return material;
        }

        private static void RemovePrimitiveCollider(
            GameObject gameObject)
        {
            if (gameObject == null)
                return;

            Component[] components =
                gameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                if (!component.GetType().Name.EndsWith("Collider"))
                    continue;

                try
                {
                    UnityEngine.Object.Destroy(component);
                }
                catch
                {
                }
            }
        }

        private static void MoveSourceOffscreen(GameObject source)
        {
            if (source == null)
                return;

            source.transform.position =
                new Vector3(0f, -22000f, 0f);

            source.SetActive(true);

            UnityEngine.Object.DontDestroyOnLoad(source);
        }

        private static Il2CppScheduleOne.ItemFramework.ItemDefinition
            GetRawDefinition(string itemId)
        {
            ItemDefinition wrapper =
                ItemManager.GetDefinition(itemId);

            if (wrapper == null)
                return null;

            object raw =
                GetMemberValue(wrapper, "S1ItemDefinition");

            return raw as
                Il2CppScheduleOne.ItemFramework.ItemDefinition;
        }

        private static bool TrySetMember(
            object target,
            string name,
            object value)
        {
            if (target == null)
                return false;

            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field =
                    type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly
                    );

                if (field != null &&
                    (value == null ||
                     field.FieldType.IsAssignableFrom(
                         value.GetType())))
                {
                    try
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                    catch
                    {
                    }
                }

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
                    (value == null ||
                     property.PropertyType.IsAssignableFrom(
                         value.GetType())))
                {
                    try
                    {
                        property.SetValue(target, value);
                        return true;
                    }
                    catch
                    {
                    }
                }

                type = type.BaseType;
            }

            return false;
        }

        private static object GetMemberValue(
            object target,
            string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property =
                        type.GetProperty(
                            name,
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.DeclaredOnly
                        );

                    if (property != null)
                    {
                        try
                        {
                            return property.GetValue(target);
                        }
                        catch
                        {
                        }
                    }

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
                        try
                        {
                            return field.GetValue(target);
                        }
                        catch
                        {
                        }
                    }

                    type = type.BaseType;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
