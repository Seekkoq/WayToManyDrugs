using System;
using System.Reflection;
using MelonLoader;
using S1API.Console;
using S1API.Items;
using S1API.Items.Ingredient;
using S1MAPI.Gltf;
using S1MAPI.Utils;
using UnityEngine;

using CustomNPCExample.Utils;

using NativeItemDefinition = Il2CppScheduleOne.ItemFramework.ItemDefinition;
using NativeStorableDefinition = Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Xanax Powder - a plain item, not a product.
    ///
    /// It is built the way the mod's other stock items are (the DMT extracts, the cookie
    /// ingredients): a clone of a vanilla ingredient definition with a custom model and icon, so
    /// it never shows up in the dealing, mixing or packaging screens. The only place it is used is
    /// the Brick Press, which the press patch teaches to accept it and turn it into Xanax.
    /// </summary>
    public static class XanaxPowder
    {
        /// <summary>Full item id, i.e. what the registry, the supplier and the press patch use.</summary>
        public const string ItemId = "westvilleconnection:ingredients/xanax_powder";

        private const string DonorId = "iodine";
        private const string GlbResource = "xanax_powder.glb";
        private const float Price = 20f;

        /// <summary>Model size in world units (the source mesh is 0.88 units across).</summary>
        private static readonly Vector3 VisualScale = Vector3.one * 0.045f;

        /// <summary>
        /// How wide the stand-in lump is built, in the same units as the mesh the item was modelled
        /// for (0.88 units across). The item's visual scale above turns that into a hand-sized lump,
        /// and the representations overwrite the visual's own scale, so the size has to be baked into
        /// the mesh rather than set on the object.
        /// </summary>
        private const float LumpAcross = 0.88f;

        private static bool _registered;
        private static bool _failed;
        private static bool _missingModelLogged;

        private static GameObject _visual;
        private static Sprite _icon;

        private static bool _iconRepairDone;
        private static float _iconRepairTimer;
        private static int _iconRepairAttempts;

        public static bool IsRegistered => _registered;

        public static bool TryRegister()
        {
            if (_registered)
                return true;

            if (_failed)
                return false;

            try
            {
                if (ItemManager.GetDefinition(DonorId) == null)
                {

                    return false;
                }

                _visual = CreatePowderBag();

                // The item itself must exist even when the mesh does not: without it the powder
                // could not be bought, given or pressed at all. A missing mesh just means the
                // powder keeps the donor's visuals until a model is dropped in.
                if (_visual == null && !_missingModelLogged)
                {
                    _missingModelLogged = true;


                }

                MixIngredientItemCreator
                    .CloneFrom(DonorId)
                    .WithBasicInfo(
                        ItemId,
                        "Xanax Powder",
                        "Unpressed alprazolam powder. A Brick Press turns it into bars.",
                        ItemCategory.Ingredient
                    )
                    .Build();

                RegisterAliasSafely("xanaxpowder", ItemId);
                RegisterAliasSafely("xanax_powder", ItemId);
                RegisterAliasSafely("pillpowder", ItemId);

                if (_visual != null)
                {
                    ApplyCustomRepresentations(ItemId, _visual);
                    MoveOffscreen(_visual);
                }

                _icon = CreatePowderIconSprite("WVC_XanaxPowder_Icon");
                WvcIcon.Apply(ItemId, _icon);

                SetPrice(ItemId, Price);

                _registered = true;



                return true;
            }
            catch (Exception ex)
            {
                _failed = true;

                MelonLogger.Error("[WVC Xanax Powder] Registration failed: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Loads the generated pouch mesh and gives it a lit material, so the item has a real 3D
        /// model in the hand, in storage and on the ground.
        /// </summary>
        private static GameObject CreatePowderBag()
        {
            try
            {
                Shader shader =
                    Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Sprites/Default");

                if (shader == null)
                {

                    return null;
                }

                GameObject model = null;

                byte[] bytes = LoadEmbeddedGlb();

                if (bytes != null)
                    model = GltfLoader.LoadGlb(bytes, shader);

                if (model == null)
                {
                    // The shipped model cannot be read - the loader cannot parse the file that is
                    // embedded - so the powder gets a plain white box instead of the donor's iodine
                    // bottle. A box is not the model, but it is unmistakably the powder, which the
                    // iodine never was: it looked like the wrong item in the hand. The GLB is still
                    // tried first, so dropping a readable one in takes over by itself.
                    if (!_missingModelLogged)
                    {
                        _missingModelLogged = true;


                    }

                    return CreatePowderLump(shader);
                }

                model.name = "WVC_XanaxPowder_Model";
                model.SetActive(true);

                // The GLB ships without materials, so every renderer gets the powder colour.
                Color bag = new Color(0.93f, 0.93f, 0.90f);

                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;

                    Material material = renderer.sharedMaterial;

                    if (material == null || material.shader == null)
                    {
                        material = new Material(shader);
                        material.name = "WVC_XanaxPowder_Material";
                        renderer.sharedMaterial = material;
                    }

                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", bag);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", bag);
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.35f);
                }

                UnityEngine.Object.DontDestroyOnLoad(model);

                return model;
            }
            catch (Exception)
            {

                return null;
            }
        }

        /// <summary>
        /// A lump of powder, for while the model cannot be read.
        ///
        /// The heap is the DMT product's own mesh, reused rather than re-modelled, so the powder
        /// reads as the same white mound DMT does instead of the iodine bottle it used to fall back
        /// to. DMT models that heap at a size of its own, so the vertices are scaled to the size
        /// this item's mesh was documented at. The GLB is still tried first, so a working model
        /// takes over by itself.
        /// </summary>
        private static GameObject CreatePowderLump(Shader shader)
        {
            try
            {
                Mesh mound = CreateLumpMesh(LumpAcross);

                if (mound == null)
                    return null;

                GameObject lump = new GameObject("WVC_XanaxPowder_Lump");

                MeshFilter filter = lump.AddComponent<MeshFilter>();
                MeshRenderer renderer = lump.AddComponent<MeshRenderer>();

                filter.sharedMesh = mound;

                Material material = new Material(shader);
                material.name = "WVC_XanaxPowder_Material";

                Color powder = new Color(0.95f, 0.95f, 0.94f);

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", powder);
                if (material.HasProperty("_Color")) material.SetColor("_Color", powder);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.25f);

                renderer.sharedMaterial = material;

                lump.transform.localScale = Vector3.one;
                lump.transform.position = new Vector3(0f, -20000f, 0f);

                UnityEngine.Object.DontDestroyOnLoad(lump);

                return lump;
            }
            catch (Exception)
            {


                return null;
            }
        }

        /// <summary>
        /// Copies DMT's powder mound and scales its vertices so the heap is the requested width.
        ///
        /// The copy matters: the mound is a shared asset and DMT still uses it, so its vertices are
        /// never touched. Measuring the source first, rather than assuming its size, means a change
        /// to DMT's constants cannot silently resize this lump.
        /// </summary>
        private static Mesh CreateLumpMesh(float across)
        {
            Mesh source = DMT.CreatePowderMoundMesh();

            if (source == null)
                return null;

            float sourceAcross = source.bounds.size.x;
            float scale = sourceAcross > 0.0001f ? across / sourceAcross : 1f;

            Vector3[] sourceVertices = source.vertices;
            Vector3[] vertices = new Vector3[sourceVertices.Length];

            for (int i = 0; i < sourceVertices.Length; i++)
                vertices[i] = sourceVertices[i] * scale;

            Mesh mesh = new Mesh();
            mesh.name = "WVC_XanaxPowder_LumpMesh";
            mesh.vertices = vertices;
            mesh.triangles = source.triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void ApplyCustomRepresentations(string itemId, GameObject customModel)
        {
            try
            {
                NativeItemDefinition definition = GetRawDefinition(itemId);

                if (definition == null)
                    return;

                ApplyEquippableRepresentation(definition, itemId, customModel, VisualScale);
                ApplyStationRepresentation(definition, itemId, customModel, VisualScale);
                ApplyStoredRepresentation(definition, itemId, customModel, VisualScale);
            }
            catch (Exception)
            {

            }
        }

        private static bool ApplyEquippableRepresentation(
            NativeItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                if (definition.Equippable == null || definition.Equippable.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    definition.Equippable.gameObject, itemId, "Equippable", customModel, visualScale);

                if (clone == null)
                    return false;

                var equippable = clone.GetComponent<Il2CppScheduleOne.Equipping.Equippable>();

                if (equippable == null)
                    return false;

                definition.Equippable = equippable;
                return true;
            }
            catch
            {
                return false;
            }
        }


        private static bool ApplyStationRepresentation(
            NativeItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                NativeStorableDefinition storable = definition.TryCast<NativeStorableDefinition>();

                if (storable == null || storable.StationItem == null || storable.StationItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StationItem.gameObject, itemId, "StationItem", customModel, visualScale);

                if (clone == null)
                    return false;

                var comp = clone.GetComponent<Il2CppScheduleOne.StationFramework.StationItem>();

                if (comp == null)
                    return false;

                storable.StationItem = comp;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplyStoredRepresentation(
            NativeItemDefinition definition,
            string itemId,
            GameObject customModel,
            Vector3 visualScale)
        {
            try
            {
                NativeStorableDefinition storable = definition.TryCast<NativeStorableDefinition>();

                if (storable == null || storable.StoredItem == null || storable.StoredItem.gameObject == null)
                    return false;

                GameObject clone = BuildRepresentationClone(
                    storable.StoredItem.gameObject, itemId, "StoredItem", customModel, visualScale);

                if (clone == null)
                    return false;

                var comp = clone.GetComponent<Il2CppScheduleOne.Storage.StoredItem>();

                if (comp == null)
                    return false;

                storable.StoredItem = comp;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clones one of the donor's representations, hides its own meshes and hangs the powder
        /// model underneath at the requested scale.
        /// </summary>
        private static GameObject BuildRepresentationClone(
            GameObject template,
            string itemId,
            string context,
            GameObject customModel,
            Vector3 localScale)
        {
            if (template == null || customModel == null)
                return null;

            GameObject clone = UnityEngine.Object.Instantiate(template);
            string safeId = itemId.Replace(":", "_").Replace("/", "_");

            clone.name = "WVC_XanaxPowder_" + context + "_" + safeId;
            clone.SetActive(true);
            clone.transform.position = new Vector3(0f, -20000f, 0f);
            UnityEngine.Object.DontDestroyOnLoad(clone);

            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }

            GameObject visual = UnityEngine.Object.Instantiate(customModel);
            visual.name = "WVC_XanaxPowder_Visual_" + context;
            visual.transform.SetParent(clone.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.SetActive(true);

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            return clone;
        }

        private static void MoveOffscreen(GameObject visual)
        {
            try
            {
                if (visual != null)
                    visual.transform.position = new Vector3(0f, -20000f, 0f);
            }
            catch
            {
            }
        }

        private static void SetPrice(string itemId, float price)
        {
            try
            {
                NativeStorableDefinition storable =
                    GetRawDefinition(itemId)?.TryCast<NativeStorableDefinition>();

                if (storable == null)
                    return;

                storable.BasePurchasePrice = price;
            }
            catch (Exception)
            {

            }
        }

        public static NativeItemDefinition GetRawDefinition(string itemId)
        {
            try
            {
                ItemDefinition wrapper = ItemManager.GetDefinition(itemId);

                if (wrapper == null)
                    return null;

                object raw = GetMemberValue(wrapper, "S1ItemDefinition");

                return raw as NativeItemDefinition;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the generated mesh inside this assembly.
        ///
        /// S1MAPI's loader asks for the exact resource name, but an embedded file is registered
        /// under its full path ("WayToManyDrugs.Assets.xanax_powder.glb"), so the manifest is swept
        /// for a matching suffix the same way the mod's product builder does it.
        /// </summary>
        private static byte[] LoadEmbeddedGlb()
        {
            try
            {
                var assembly = typeof(XanaxPowder).Assembly;

                byte[] bytes = EmbeddedResourceLoader.LoadBytes(GlbResource, assembly);

                if (bytes != null)
                    return bytes;

                foreach (string name in assembly.GetManifestResourceNames())
                {
                    if (!name.EndsWith(GlbResource, StringComparison.OrdinalIgnoreCase))
                        continue;

                    using (var stream = assembly.GetManifestResourceStream(name))
                    {
                        if (stream == null)
                            continue;

                        bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);



                        return bytes;
                    }
                }
            }
            catch (Exception)
            {

            }

            return null;
        }

        /// <summary>
        /// S1API keeps the native definition behind a member that has moved name between S1API
        /// builds, so it is read reflectively the same way the other items in this mod do it.
        /// </summary>
        private static object GetMemberValue(object target, string name)
        {
            if (target == null)
                return null;

            try
            {
                Type type = target.GetType();

                while (type != null)
                {
                    PropertyInfo property = type.GetProperty(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

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

                    FieldInfo field = type.GetField(
                        name,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

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

        private static void RegisterAliasSafely(string alias, string itemId)
        {
            try
            {
                ConsoleItemAliases.Register(alias, itemId);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Hand-drawn icon: a heap of white powder, matching the lump the item is modelled as.
        ///
        /// Drawn rather than rendered from the model, because the model is a stand-in and an icon
        /// rendered from it comes out as a blank white blob. The heap is shaded and rimmed so it
        /// still reads as powder against the pale panel the inventory draws, which a plain white
        /// shape does not.
        /// </summary>
        private static Sprite CreatePowderIconSprite(string name)
        {
            const int size = 512;
            const int grainCount = 20;

            float centerX = size * 0.5f;
            float baseY = 172f;
            float radiusX = 150f;
            float radiusY = 196f;

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color top = new Color(1.00f, 1.00f, 0.99f, 1f);
            Color mid = new Color(0.93f, 0.93f, 0.92f, 1f);
            Color body = new Color(0.79f, 0.79f, 0.78f, 1f);
            Color rim = new Color(0.40f, 0.40f, 0.44f, 1f);
            Color white = new Color(1f, 1f, 1f, 1f);
            Color speck = new Color(0.86f, 0.86f, 0.85f, 1f);
            Color shadow = new Color(0.20f, 0.20f, 0.24f, 1f);

            // Grains lying around the base. Placed from a fixed seed so the icon is identical every
            // run, and kept small so they read as spill rather than as a second shape.
            float[] grainX = new float[grainCount];
            float[] grainY = new float[grainCount];
            float[] grainRadius = new float[grainCount];

            uint seed = 987654321u;

            for (int i = 0; i < grainCount; i++)
            {
                seed = seed * 1103515245u + 12345u;
                float u1 = ((seed >> 16) & 0x7fff) / 32767f;

                seed = seed * 1103515245u + 12345u;
                float u2 = ((seed >> 16) & 0x7fff) / 32767f;

                grainX[i] = centerX + (u1 * 2f - 1f) * radiusX * 1.22f;
                grainY[i] = baseY + 4f + u2 * 16f;
                grainRadius[i] = 2f + u2 * 2.2f;
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + "_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centerX;
                    float dy = y - baseY;

                    Color pixel = clear;

                    // Cast shadow: a flattened ellipse under the heap, so it sits on the ground
                    // instead of floating on the panel.
                    float shadowX = dx / (radiusX * 1.14f);
                    float shadowY = (y - (baseY - 4f)) / (radiusY * 0.13f);
                    float shadowDistance = Mathf.Sqrt(shadowX * shadowX + shadowY * shadowY) - 1f;

                    if (shadowDistance < 0f)
                    {
                        pixel = WvcIcon.Over(
                            new Color(
                                shadow.r,
                                shadow.g,
                                shadow.b,
                                Mathf.Clamp01(-shadowDistance * 3.2f) * 0.3f),
                            pixel);
                    }

                    // The heap: half an ellipse above the base line, so its underside is flat.
                    if (y >= baseY)
                    {
                        float angle = Mathf.Atan2(dy, dx);
                        float bump =
                            1f +
                            0.038f * Mathf.Sin(angle * 3.1f) +
                            0.022f * Mathf.Sin(angle * 5.7f + 1.3f);

                        float normalisedX = dx / radiusX;
                        float normalisedY = dy / radiusY;

                        float radial =
                            Mathf.Sqrt(normalisedX * normalisedX + normalisedY * normalisedY) / bump;

                        float distance = (radial - 1f) * Mathf.Min(radiusX, radiusY);

                        if (distance < 1.5f)
                        {
                            float height = Mathf.Clamp01(dy / radiusY);

                            Color colour = Color.Lerp(
                                Color.Lerp(body, mid, Mathf.Clamp01(height * 1.6f)),
                                top,
                                Mathf.Clamp01((height - 0.35f) / 0.65f));

                            // Lit from the upper left, so the heap has a lit side and a shaded one
                            // rather than being one flat white shape.
                            float lightX = (dx + 42f) / (radiusX * 1.05f);
                            float lightY = (y - (baseY + radiusY * 0.58f)) / (radiusY * 0.95f);
                            float light =
                                Mathf.Clamp01(1f - Mathf.Sqrt(lightX * lightX + lightY * lightY));

                            colour = Color.Lerp(colour, white, light * 0.38f);

                            if (((x * 13 + y * 7) % 97) < 3)
                                colour = Color.Lerp(colour, speck, 0.45f);

                            if (distance > -2f)
                                colour = Color.Lerp(colour, rim, Mathf.Clamp01((distance + 2f) / 2f));

                            colour.a = Mathf.Clamp01(0.5f - distance);

                            pixel = WvcIcon.Over(colour, pixel);
                        }
                    }

                    for (int i = 0; i < grainCount; i++)
                    {
                        float grainOffsetX = x - grainX[i];
                        float grainOffsetY = y - grainY[i];

                        float grainDistance =
                            Mathf.Sqrt(grainOffsetX * grainOffsetX + grainOffsetY * grainOffsetY) -
                            grainRadius[i];

                        if (grainDistance >= 1.2f)
                            continue;

                        Color grain = Color.Lerp(
                            mid,
                            body,
                            Mathf.Clamp01((grainOffsetY / grainRadius[i] + 1f) * 0.5f));

                        if (grainDistance > -1.2f)
                            grain = Color.Lerp(grain, rim, Mathf.Clamp01((grainDistance + 1.2f) / 1.2f));

                        grain.a = Mathf.Clamp01(0.5f - grainDistance);

                        pixel = WvcIcon.Over(grain, pixel);
                    }

                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            UnityEngine.Object.DontDestroyOnLoad(texture);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);

            sprite.name = name;
            UnityEngine.Object.DontDestroyOnLoad(sprite);

            return sprite;
        }

        private const float IconRepairInterval = 5f;
        private const int MaxIconRepairAttempts = 12;

        /// <summary>
        /// Keeps the drawn powder icon on the item.
        ///
        /// The game generates icons of its own for storable items, and it generates them from the
        /// item's model - which for the powder is the stand-in lump, so its version comes out as a
        /// blank white shape. Re-asserting the drawn sprite over the window in which the game does
        /// its generating means whichever lands last, the drawn one is what the inventory shows.
        /// Bounded, because after that window nothing is going to overwrite it again.
        /// </summary>
        public static void UpdateIconRepair()
        {
            if (_iconRepairDone || !_registered || _icon == null)
                return;

            _iconRepairTimer += Time.deltaTime;

            if (_iconRepairTimer < IconRepairInterval)
                return;

            _iconRepairTimer = 0f;
            _iconRepairAttempts++;

            try
            {
                WvcIcon.Apply(ItemId, _icon);
            }
            catch (Exception)
            {

                _iconRepairDone = true;
                return;
            }

            if (_iconRepairAttempts >= MaxIconRepairAttempts)
                _iconRepairDone = true;
        }

    }
}
