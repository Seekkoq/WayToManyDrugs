using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

using Il2CppSeedDefinition =
    Il2CppScheduleOne.Growing.SeedDefinition;

using Il2CppPlant =
    Il2CppScheduleOne.Growing.Plant;

using Il2CppWeedPlant =
    Il2CppScheduleOne.Growing.WeedPlant;

using Il2CppPlantHarvestable =
    Il2CppScheduleOne.Growing.PlantHarvestable;

using Il2CppStorableDef =
    Il2CppScheduleOne.ItemFramework.StorableItemDefinition;

using Il2CppEquippable =
    Il2CppScheduleOne.Equipping.Equippable;

using Il2CppStoredItem =
    Il2CppScheduleOne.Storage.StoredItem;

using Il2CppItemInstance =
    Il2CppScheduleOne.ItemFramework.ItemInstance;

namespace CustomNPCExample.Products
{
    public static class SalviaGrowing
    {
        private static GameObject _prefabHolder;
        private static GameObject _plantClone;
        private static GameObject _functionalSeedClone;

        private static Il2CppStorableDef _cachedProduct;
        private const float SalviaLabelUOffset = 0f;

        private static readonly Color SalviaLabelColor =
            new Color(0.85f, 0.20f, 0.60f, 1f);

        private static readonly Dictionary<string, Material> _labelVariantCache =
            new Dictionary<string, Material>();

        private static Material _fallbackLabelMaterial;

        public static string DonorSeedId { get; private set; }

        public static GameObject PlantPrefab => _plantClone;
        public static GameObject FunctionalSeedPrefab => _functionalSeedClone;

        private static readonly Color SalviaPlantTint =
            new Color(0.46f, 0.66f, 0.34f, 1f);

        public static Il2CppStorableDef FindNativeProduct(string id)
        {
            if (_cachedProduct != null && _cachedProduct.ID == id)
                return _cachedProduct;

            foreach (var d in Resources.FindObjectsOfTypeAll<Il2CppStorableDef>())
            {
                if (d != null && d.ID == id)
                {
                    _cachedProduct = d;
                    return d;
                }
            }

            return null;
        }

        public static Il2CppSeedDefinition FindDonorSeed()
        {
            var seeds = Resources.FindObjectsOfTypeAll<Il2CppSeedDefinition>();

            foreach (var seed in seeds)
            {
                if (seed == null ||
                    seed.PlantPrefab == null ||
                    seed.FunctionSeedPrefab == null)
                {
                    continue;
                }

                string id = seed.ID == null ? string.Empty : seed.ID.ToLowerInvariant();
                string name = seed.Name == null ? string.Empty : seed.Name.ToLowerInvariant();

                if ((id.Contains("og") && id.Contains("kush")) ||
                    name.Contains("og kush"))
                {
                    DonorSeedId = seed.ID;
                    global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Salvia] OG Kush seed donor found: " + DonorSeedId);
                    return seed;
                }
            }

            foreach (var seed in seeds)
            {
                if (seed != null &&
                    seed.PlantPrefab != null &&
                    seed.FunctionSeedPrefab != null)
                {
                    DonorSeedId = seed.ID;

                    return seed;
                }
            }

            return null;
        }

        public static bool BuildPrefabs(
            Il2CppSeedDefinition donor,
            Il2CppStorableDef salviaProduct)
        {
            if (_plantClone != null && _functionalSeedClone != null)
                return true;

            if (donor == null ||
                donor.PlantPrefab == null ||
                donor.FunctionSeedPrefab == null ||
                salviaProduct == null)
            {
                return false;
            }

            EnsureHolder();

            _plantClone = UnityEngine.Object.Instantiate(
                donor.PlantPrefab.gameObject,
                _prefabHolder.transform
            );
            _plantClone.name = "WVC_SalviaPlant";

            Il2CppPlant plant = _plantClone.GetComponent<Il2CppPlant>();
            if (plant == null)
                return false;

            plant.GrowthTime =
                Mathf.RoundToInt(donor.PlantPrefab.GrowthTime * 1.6f);
            plant.BaseYieldQuantity =
                Mathf.Max(1, donor.PlantPrefab.BaseYieldQuantity - 4);

            plant.HarvestTarget = "Salvia";

            int retargeted = 0;
            retargeted += RetargetHarvestables(_plantClone, salviaProduct);
            retargeted += RetargetBranchPrefab(plant, salviaProduct);
            TintPlant(_plantClone);
            ReplacePlantVisualsWithObj(_plantClone);

            _functionalSeedClone = UnityEngine.Object.Instantiate(
                donor.FunctionSeedPrefab.gameObject,
                _prefabHolder.transform
            );
            _functionalSeedClone.name = "WVC_SalviaFunctionalSeed";

            int labelled = ApplySalviaLabel(_functionalSeedClone);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Salvia] Prefabs built. " +
                "retargetedHarvestables=" + retargeted +
                " labelledSeedSlots=" + labelled +
                " growthTime=" + plant.GrowthTime +
                " baseYield=" + plant.BaseYieldQuantity
            );

            return true;
        }

        public static void LinkSeedDefinition(Il2CppSeedDefinition salviaSeed)
        {
            if (_plantClone == null || salviaSeed == null)
                return;

            Il2CppPlant plant = _plantClone.GetComponent<Il2CppPlant>();
            if (plant != null)
                plant.SeedDefinition = salviaSeed;
        }

        public static bool IsSalviaPlant(Il2CppPlant plant)
        {
            if (plant == null)
                return false;

            try
            {
                if (plant.SeedDefinition != null &&
                    plant.SeedDefinition.ID == SalviaSeed.SeedId)
                    return true;
            }
            catch { }

            try
            {
                if (plant.HarvestTarget == "Salvia" ||
                    plant.HarvestTarget == Salvia.ProductId)
                    return true;
            }
            catch { }

            try
            {
                if (plant.gameObject != null &&
                    plant.gameObject.name.IndexOf("Salvia", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch { }

            return false;
        }

        public static bool RetargetHarvestableIfSalvia(Il2CppPlantHarvestable harvestable)
        {
            if (harvestable == null)
                return false;

            Il2CppStorableDef product = FindNativeProduct(Salvia.ProductId);
            if (product == null)
                return false;

            try
            {
                if (harvestable.Product != null &&
                    harvestable.Product.ID == product.ID)
                    return false;
            }
            catch { }

            Il2CppPlant parent = null;
            try { parent = harvestable.GetComponentInParent<Il2CppPlant>(); }
            catch { }

            if (!IsSalviaPlant(parent))
                return false;

            harvestable.Product = product;
            if (harvestable.ProductQuantity <= 0)
                harvestable.ProductQuantity = 1;

            return true;
        }

        public static Il2CppItemInstance CreateSalviaInstance(int quantity)
        {
            Il2CppStorableDef product = FindNativeProduct(Salvia.ProductId);
            if (product == null)
                return null;

            return product.GetDefaultInstance(Mathf.Max(1, quantity));
        }

        public static int ApplySalviaLabel(GameObject root)
        {
            if (root == null)
                return 0;

            int changed = 0;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                string rn = renderer.name == null ? string.Empty : renderer.name.ToLowerInvariant();
                if (rn.Contains("sphere") ||
                    rn.Contains("pellet") ||
                    rn.Contains("grain") ||
                    rn == "seed")
                {
                    continue;
                }

                Material[] mats = renderer.sharedMaterials;
                bool any = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material source = mats[i];
                    if (source == null)
                        continue;

                    if (!LooksLikeSeedLabel(renderer, source))
                        continue;

                    Rect uvRect;
                    if (!TryGetUvRect(renderer, i, out uvRect))
                        uvRect = new Rect(0f, 0f, 1f, 1f);

                    mats[i] = GetSalviaLabelVariant(source, uvRect);
                    changed++;
                    any = true;
                }

                if (any)
                    renderer.sharedMaterials = mats;
            }

            return changed;
        }

        private static bool LooksLikeSeedLabel(Renderer renderer, Material material)
        {
            string rn = renderer.name == null ? string.Empty : renderer.name.ToLowerInvariant();
            string mn = material.name == null ? string.Empty : material.name.ToLowerInvariant();

            if (rn.Contains("sphere") ||
                rn.Contains("pellet") ||
                rn.Contains("grain") ||
                rn == "seed")
            {
                return false;
            }

            if (mn.Contains("pellet") ||
                mn.Contains("grain") ||
                mn.Contains("seed_mat"))
            {
                return false;
            }

            if (mn.Contains("pellet") || mn.Contains("grain") || mn.Contains("seed_mat"))
                return false;

            if (mn.Contains("wvc_salvialabel"))
                return false;

            if (rn.Contains("label") || rn.Contains("sticker") || rn.Contains("decal") || rn.Contains("wrapper"))
                return true;

            if (mn.Contains("label") || mn.Contains("sticker") || mn.Contains("decal") || mn.Contains("wrapper"))
                return true;

            if (mn.Contains("og") && mn.Contains("kush") && !mn.Contains("seed"))
                return true;

            return false;
        }

        private static bool TryGetUvRect(Renderer renderer, int slotIndex, out Rect rect)
        {
            rect = new Rect(0f, 0f, 1f, 1f);
            Mesh mesh = null;

            try
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null)
                    mesh = filter.sharedMesh;

                if (mesh == null)
                {
                    SkinnedMeshRenderer skinned = renderer.TryCast<SkinnedMeshRenderer>();
                    if (skinned != null)
                        mesh = skinned.sharedMesh;
                }
            }
            catch { }

            if (mesh == null || slotIndex < 0 || slotIndex >= mesh.subMeshCount)
                return false;

            var uvs = mesh.uv;
            if (uvs == null || uvs.Length == 0)
                return false;

            var tris = mesh.GetTriangles(slotIndex);
            if (tris == null || tris.Length == 0)
                return false;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < tris.Length; i++)
            {
                int vi = tris[i];
                if (vi < 0 || vi >= uvs.Length)
                    continue;

                Vector2 uv = uvs[vi];
                if (uv.x < minX) minX = uv.x;
                if (uv.x > maxX) maxX = uv.x;
                if (uv.y < minY) minY = uv.y;
                if (uv.y > maxY) maxY = uv.y;
            }

            if (minX > maxX || minY > maxY)
                return false;

            rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            return true;
        }

        private static Material GetSalviaLabelVariant(
            Material source,
            Rect uvRect)
        {
            if (source == null)
                return GetFallbackLabelMaterial();

            string key =
                source.GetInstanceID() +
                "|salvia_solid_label_v4";

            Material cached;

            if (_labelVariantCache.TryGetValue(key, out cached) &&
                cached != null)
            {
                return cached;
            }

            Material variant = new Material(source);
            variant.name = source.name + "_WVC_SalviaSolidLabel";

            if (variant.HasProperty("_BaseColor"))
                variant.SetColor("_BaseColor", SalviaLabelColor);

            if (variant.HasProperty("_Color"))
                variant.SetColor("_Color", SalviaLabelColor);

            if (variant.HasProperty("_BaseMap"))
            {
                variant.SetTexture("_BaseMap", null);
                variant.SetTextureScale("_BaseMap", Vector2.one);
                variant.SetTextureOffset("_BaseMap", Vector2.zero);
            }

            if (variant.HasProperty("_MainTex"))
            {
                variant.SetTexture("_MainTex", null);
                variant.SetTextureScale("_MainTex", Vector2.one);
                variant.SetTextureOffset("_MainTex", Vector2.zero);
            }

            _labelVariantCache[key] = variant;

            return variant;
        }

        private static void ApplyNormalizedLabelTexture(
    Material material,
    string propertyName,
    Texture2D texture)
        {
            if (material == null ||
                !material.HasProperty(propertyName))
            {
                return;
            }

            material.SetTexture(propertyName, texture);

            material.SetTextureScale(
                propertyName,
                Vector2.one
            );

            material.SetTextureOffset(
                propertyName,
                new Vector2(SalviaLabelUOffset, 0f)
            );
        }

        private static Material GetFallbackLabelMaterial()
        {
            if (_fallbackLabelMaterial != null)
                return _fallbackLabelMaterial;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            _fallbackLabelMaterial =
                new Material(shader);

            _fallbackLabelMaterial.name =
                "WVC_SalviaSeed_SolidLabel";

            if (_fallbackLabelMaterial.HasProperty("_BaseColor"))
                _fallbackLabelMaterial.SetColor(
                    "_BaseColor",
                    SalviaLabelColor
                );

            if (_fallbackLabelMaterial.HasProperty("_Color"))
                _fallbackLabelMaterial.SetColor(
                    "_Color",
                    SalviaLabelColor
                );

            if (_fallbackLabelMaterial.HasProperty("_BaseMap"))
            {
                _fallbackLabelMaterial.SetTexture("_BaseMap", null);
                _fallbackLabelMaterial.SetTextureScale("_BaseMap", Vector2.one);
                _fallbackLabelMaterial.SetTextureOffset("_BaseMap", Vector2.zero);
            }

            if (_fallbackLabelMaterial.HasProperty("_MainTex"))
            {
                _fallbackLabelMaterial.SetTexture("_MainTex", null);
                _fallbackLabelMaterial.SetTextureScale("_MainTex", Vector2.one);
                _fallbackLabelMaterial.SetTextureOffset("_MainTex", Vector2.zero);
            }

            return _fallbackLabelMaterial;
        }

        private static int RetargetHarvestables(
            GameObject root,
            Il2CppStorableDef salviaProduct)
        {
            if (root == null || salviaProduct == null)
                return 0;

            int count = 0;
            foreach (var h in root.GetComponentsInChildren<Il2CppPlantHarvestable>(true))
            {
                if (h == null) continue;
                h.Product = salviaProduct;
                if (h.ProductQuantity <= 0) h.ProductQuantity = 1;
                count++;
            }
            return count;
        }

        private static int RetargetBranchPrefab(
            Il2CppPlant plant,
            Il2CppStorableDef salviaProduct)
        {
            if (plant == null || salviaProduct == null)
                return 0;

            Il2CppWeedPlant weedPlant = plant.TryCast<Il2CppWeedPlant>();
            if (weedPlant == null || weedPlant.BranchPrefab == null)
                return 0;

            try
            {
                if (weedPlant.BranchPrefab.Product != null &&
                    weedPlant.BranchPrefab.Product.ID == salviaProduct.ID)
                    return 1;
            }
            catch { }

            GameObject branchClone = UnityEngine.Object.Instantiate(
                weedPlant.BranchPrefab.gameObject,
                _prefabHolder.transform
            );
            branchClone.name = "WVC_SalviaBranch";

            int count = RetargetHarvestables(branchClone, salviaProduct);
            TintPlant(branchClone);

            Il2CppPlantHarvestable harvestable = branchClone.GetComponent<Il2CppPlantHarvestable>();
            if (harvestable != null)
            {
                harvestable.Product = salviaProduct;
                if (harvestable.ProductQuantity <= 0) harvestable.ProductQuantity = 1;
                weedPlant.BranchPrefab = harvestable;
                if (count == 0) count = 1;
            }

            return count;
        }

        private static void EnsureHolder()
        {
            if (_prefabHolder != null) return;
            _prefabHolder = new GameObject("WVC_SalviaPrefabHolder");
            _prefabHolder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_prefabHolder);
        }

        private static bool _hierarchyDumped;

        private static void DumpPlantHierarchy(GameObject node, int depth)
        {
            if (node == null || depth > 4)
                return;

            string indent = new string(' ', depth * 2);

            int count = node.transform.childCount;

            for (int i = 0; i < count; i++)
            {
                Transform child;

                try
                {
                    child = node.transform.GetChild(i);
                }
                catch
                {
                    continue;
                }

                if (child == null)
                    continue;

                string rendererInfo = "";
                try
                {
                    var mr = child.GetComponent<MeshRenderer>();
                    var smr = child.GetComponent<SkinnedMeshRenderer>();
                    var harvestable = child.GetComponent<Il2CppPlantHarvestable>();

                    if (smr != null) rendererInfo = " [SkinnedMesh]";
                    else if (mr != null) rendererInfo = " [Mesh]";
                    if (harvestable != null) rendererInfo += " [Harvestable]";
                }
                catch
                {
                }



                DumpPlantHierarchy(child.gameObject, depth + 1);
            }
        }

        private static void ReplacePlantVisualsWithObj(GameObject plantClone)
        {
            return;
        }

        private static void ReplacePlantVisualsWithObjCore(GameObject plantClone)
        {
            try
            {
                SalviaObjModel plantModel = SalviaObjModels.Plant;

                if (plantModel == null ||
                    plantModel.Mesh == null ||
                    plantClone == null)
                    return;

                Mesh objMesh = plantModel.Mesh;
                Material[] plantMats = plantModel.GetMaterials();

                MeshRenderer[] renderers =
                    plantClone.GetComponentsInChildren<MeshRenderer>(true);

                int replaced = 0;

                foreach (MeshRenderer renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    try
                    {
                        Il2CppPlantHarvestable harvestable =
                            renderer.GetComponentInParent<Il2CppPlantHarvestable>();

                        if (harvestable != null)
                            continue;
                    }
                    catch
                    {
                    }

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();

                    if (filter == null || filter.sharedMesh == null)
                        continue;

                    Mesh original = filter.sharedMesh;
                    Bounds originalBounds = original.bounds;

                    float origX = Mathf.Max(0.001f, originalBounds.size.x);
                    float origY = Mathf.Max(0.001f, originalBounds.size.y);
                    float origZ = Mathf.Max(0.001f, originalBounds.size.z);

                    Bounds objBounds = objMesh.bounds;
                    float scaleX = origX / Mathf.Max(0.001f, objBounds.size.x);
                    float scaleY = origY / Mathf.Max(0.001f, objBounds.size.y);
                    float scaleZ = origZ / Mathf.Max(0.001f, objBounds.size.z);

                    Mesh fitted = UnityEngine.Object.Instantiate(objMesh);
                    fitted.name = "WVC_Salvia_Obj_" + original.name;

                    Vector3[] verts = fitted.vertices;

                    for (int i = 0; i < verts.Length; i++)
                    {
                        verts[i] = Vector3.Scale(verts[i], new Vector3(scaleX, scaleY, scaleZ));
                    }

                    fitted.vertices = verts;
                    fitted.RecalculateBounds();

                    filter.sharedMesh = fitted;
                    renderer.sharedMaterials = plantMats;
                    replaced++;
                }

                SkinnedMeshRenderer[] skinned =
                    plantClone.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                foreach (SkinnedMeshRenderer skinnedRenderer in skinned)
                {
                    if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
                        continue;

                    try
                    {
                        Il2CppPlantHarvestable harvestable =
                            skinnedRenderer.GetComponentInParent<Il2CppPlantHarvestable>();

                        if (harvestable != null)
                            continue;
                    }
                    catch
                    {
                    }

                    Mesh original = skinnedRenderer.sharedMesh;
                    Bounds originalBounds = original.bounds;

                    float origX = Mathf.Max(0.001f, originalBounds.size.x);
                    float origY = Mathf.Max(0.001f, originalBounds.size.y);
                    float origZ = Mathf.Max(0.001f, originalBounds.size.z);

                    Bounds objBounds = objMesh.bounds;
                    float scaleX = origX / Mathf.Max(0.001f, objBounds.size.x);
                    float scaleY = origY / Mathf.Max(0.001f, objBounds.size.y);
                    float scaleZ = origZ / Mathf.Max(0.001f, objBounds.size.z);

                    Mesh fitted = UnityEngine.Object.Instantiate(objMesh);
                    fitted.name = "WVC_Salvia_ObjSkinned_" + original.name;

                    Vector3[] verts = fitted.vertices;

                    for (int i = 0; i < verts.Length; i++)
                    {
                        verts[i] = Vector3.Scale(verts[i], new Vector3(scaleX, scaleY, scaleZ));
                    }

                    fitted.vertices = verts;
                    fitted.RecalculateBounds();

                    skinnedRenderer.sharedMesh = fitted;
                    skinnedRenderer.sharedMaterials = plantMats;
                    replaced++;
                }

                replaced += ReplaceHarvestableVisualsWithObj(plantClone);



                if (!_hierarchyDumped)
                {
                    _hierarchyDumped = true;
                    DumpPlantHierarchy(plantClone, 0);
                }
            }
            catch (Exception)
            {

            }
        }

        private static void TintPlant(GameObject root)
        {
            if (root == null) return;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;

                Material[] source = renderer.sharedMaterials;
                Material[] copies = new Material[source.Length];

                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] == null) continue;
                    Material material = new Material(source[i]);
                    material.name = source[i].name + "_WVC_Salvia";

                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", Color.Lerp(material.GetColor("_BaseColor"), SalviaPlantTint, 0.55f));
                    if (material.HasProperty("_Color"))
                        material.SetColor("_Color", Color.Lerp(material.GetColor("_Color"), SalviaPlantTint, 0.55f));

                    copies[i] = material;
                }
                renderer.sharedMaterials = copies;
            }
        }

        private static void FillRect(
            Texture2D tex,
            int x0,
            int y0,
            int x1,
            int y1,
            Color color)
        {
            x0 = Mathf.Clamp(x0, 0, tex.width);
            x1 = Mathf.Clamp(x1, 0, tex.width);
            y0 = Mathf.Clamp(y0, 0, tex.height);
            y1 = Mathf.Clamp(y1, 0, tex.height);

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                    tex.SetPixel(x, y, color);
            }
        }

        private static bool _productVisualsApplied;

        public static void ApplyProductItemVisuals(Il2CppStorableDef productDef)
        {
            ApplyProductDonorItemVisuals(productDef);
        }

        public static void ApplyProductDonorItemVisuals(Il2CppStorableDef productDef)
        {
            if (_productVisualsApplied)
                return;

            if (productDef == null)
                return;

            try
            {
                Il2CppStorableDef donor = FindDonorProductDef();

                if (donor == null)
                {

                    return;
                }



                EnsureHolder();

                int swapped = 0;

                try
                {
                    var equippable = donor.Equippable;

                    if (equippable != null &&
                        equippable.gameObject != null)
                    {
                        LogPrefabContents("GDP Equippable", equippable.gameObject);

                        GameObject clone = UnityEngine.Object.Instantiate(
                            equippable.gameObject,
                            _prefabHolder.transform);

                        clone.name = "WVC_Salvia_Product_Equippable";

                        var cloneEquippable = clone.GetComponent<Il2CppEquippable>();

                        if (cloneEquippable != null)
                        {
                            productDef.Equippable = cloneEquippable;
                            swapped++;
                        }
                    }
                }
                catch (Exception)
                {

                }

                try
                {
                    var storedItem = donor.StoredItem;

                    if (storedItem != null &&
                        storedItem.gameObject != null)
                    {
                        GameObject clone = UnityEngine.Object.Instantiate(
                            storedItem.gameObject,
                            _prefabHolder.transform);

                        clone.name = "WVC_Salvia_Product_StoredItem";

                        var cloneStored = clone.GetComponent<Il2CppStoredItem>();

                        if (cloneStored != null)
                        {
                            productDef.StoredItem = cloneStored;
                            swapped++;
                        }
                    }
                }
                catch (Exception)
                {

                }

                if (swapped > 0)
                {
                    _productVisualsApplied = true;


                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
        }

        private static void LogPrefabContents(string label, GameObject root)
        {
            try
            {
                var filters = root.GetComponentsInChildren<MeshFilter>(true);

                for (int i = 0; i < filters.Length; i++)
                {
                    Mesh mesh = filters[i].sharedMesh;


                }

                var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                for (int i = 0; i < skinned.Length; i++)
                {
                    Mesh mesh = skinned[i].sharedMesh;


                }
            }
            catch (Exception)
            {

            }
        }

        private static Il2CppStorableDef FindDonorProductDef()
        {
            string[] ids =
            {
                "granddaddypurple",
                "grand_daddypurple",
                "granddaddy_purple",
                "granddaddypurple_bud"
            };

            foreach (string id in ids)
            {
                Il2CppStorableDef def = FindNativeProduct(id);

                if (def != null)
                    return def;
            }

            return null;
        }

        private static GameObject _budVisualSource;

        public static GameObject GetDonorBudVisualSource()
        {
            if (_budVisualSource != null)
                return _budVisualSource;

            try
            {
                Il2CppStorableDef donor = FindDonorProductDef();

                if (donor == null ||
                    donor.Equippable == null ||
                    donor.Equippable.gameObject == null)
                    return null;

                MeshFilter[] filters =
                    donor.Equippable.gameObject.GetComponentsInChildren<MeshFilter>(true);

                MeshFilter primary = null;

                foreach (MeshFilter filter in filters)
                {
                    if (filter != null && filter.sharedMesh != null)
                    {
                        primary = filter;
                        break;
                    }
                }

                if (primary == null)
                    return null;

                GameObject go =
                    new GameObject("WVC_Salvia_GDPBud_Visual");

                MeshFilter target =
                    go.AddComponent<MeshFilter>();

                MeshRenderer renderer =
                    go.AddComponent<MeshRenderer>();

                target.sharedMesh = primary.sharedMesh;

                MeshRenderer sourceRenderer =
                    primary.GetComponent<MeshRenderer>();

                if (sourceRenderer != null)
                {
                    Material[] sourceMats =
                        sourceRenderer.sharedMaterials;

                    Material[] tinted =
                        new Material[sourceMats.Length];

                    for (int i = 0; i < sourceMats.Length; i++)
                    {
                        if (sourceMats[i] == null)
                            continue;

                        Material copy = new Material(sourceMats[i]);

                        copy.name = sourceMats[i].name + "_WVC_GDP";

                        if (copy.HasProperty("_BaseColor"))
                            copy.SetColor(
                                "_BaseColor",
                                ShiftToGdpPurple(copy.GetColor("_BaseColor")));

                        if (copy.HasProperty("_Color"))
                            copy.SetColor(
                                "_Color",
                                ShiftToGdpPurple(copy.GetColor("_Color")));

                        tinted[i] = copy;
                    }

                    renderer.sharedMaterials = tinted;
                }

                go.transform.position =
                    new Vector3(0f, -20000f, 0f);

                UnityEngine.Object.DontDestroyOnLoad(go);

                _budVisualSource = go;



                return go;
            }
            catch (Exception)
            {


                return null;
            }
        }

        private static Color ShiftToGdpPurple(Color color)
        {
            float hue;
            float saturation;
            float value;

            Color.RGBToHSV(color, out hue, out saturation, out value);

            return Color.HSVToRGB(
                0.76f,
                Mathf.Clamp(saturation, 0.45f, 0.85f),
                Mathf.Clamp(value, 0.45f, 0.9f));
        }

        public static void ApplyProductItemVisualsCore(Il2CppStorableDef productDef)
        {
            if (_productVisualsApplied)
                return;

            if (productDef == null)
                return;

            try
            {
                SalviaObjModel harvestModel = SalviaObjModels.Harvestable;

                if (harvestModel == null || harvestModel.Mesh == null)
                    return;

                EnsureHolder();

                int swapped = 0;

                try
                {
                    var equippable = productDef.Equippable;

                    if (equippable != null &&
                        equippable.gameObject != null)
                    {
                        GameObject clone = UnityEngine.Object.Instantiate(
                            equippable.gameObject,
                            _prefabHolder.transform);

                        clone.name = "WVC_Salvia_Product_Equippable";

                        swapped += ReplaceRendererMeshesWithObj(clone, harvestModel);

                        var cloneEquippable = clone.GetComponent<Il2CppEquippable>();

                        if (cloneEquippable != null)
                            productDef.Equippable = cloneEquippable;
                    }
                }
                catch (Exception)
                {

                }

                try
                {
                    var storedItem = productDef.StoredItem;

                    if (storedItem != null &&
                        storedItem.gameObject != null)
                    {
                        GameObject clone = UnityEngine.Object.Instantiate(
                            storedItem.gameObject,
                            _prefabHolder.transform);

                        clone.name = "WVC_Salvia_Product_StoredItem";

                        swapped += ReplaceRendererMeshesWithObj(clone, harvestModel);

                        var cloneStored = clone.GetComponent<Il2CppStoredItem>();

                        if (cloneStored != null)
                            productDef.StoredItem = cloneStored;
                    }
                }
                catch (Exception)
                {

                }

                if (swapped > 0)
                {
                    _productVisualsApplied = true;


                }
            }
            catch (Exception)
            {

            }
        }

        private static int ReplaceRendererMeshesWithObj(GameObject root, SalviaObjModel model)
        {
            int replaced = 0;

            Material[] materials = model.GetMaterials();

            MeshRenderer[] renderers =
                root.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();

                if (filter == null || filter.sharedMesh == null)
                    continue;

                filter.sharedMesh = BuildFittedMeshUniform(model.Mesh, filter.sharedMesh);
                renderer.sharedMaterials = materials;
                replaced++;
            }

            SkinnedMeshRenderer[] skinned =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer skinnedRenderer in skinned)
            {
                if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
                    continue;

                skinnedRenderer.sharedMesh =
                    BuildFittedMeshUniform(model.Mesh, skinnedRenderer.sharedMesh);
                skinnedRenderer.sharedMaterials = materials;
                replaced++;
            }

            return replaced;
        }

        private static int ReplaceHarvestableVisualsWithObj(GameObject plantClone)
        {
            SalviaObjModel model = SalviaObjModels.Harvestable;

            if (model == null || model.Mesh == null)
                return 0;

            int replaced = 0;

            Material[] materials = model.GetMaterials();

            MeshRenderer[] renderers =
                plantClone.GetComponentsInChildren<MeshRenderer>(true);

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                try
                {
                    if (renderer.GetComponentInParent<Il2CppPlantHarvestable>() == null)
                        continue;
                }
                catch
                {
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();

                if (filter == null || filter.sharedMesh == null)
                    continue;

                filter.sharedMesh = BuildFittedMeshUniform(model.Mesh, filter.sharedMesh);
                renderer.sharedMaterials = materials;
                replaced++;
            }

            SkinnedMeshRenderer[] skinned =
                plantClone.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer skinnedRenderer in skinned)
            {
                if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
                    continue;

                try
                {
                    if (skinnedRenderer.GetComponentInParent<Il2CppPlantHarvestable>() == null)
                        continue;
                }
                catch
                {
                    continue;
                }

                skinnedRenderer.sharedMesh =
                    BuildFittedMeshUniform(model.Mesh, skinnedRenderer.sharedMesh);
                skinnedRenderer.sharedMaterials = materials;
                replaced++;
            }

            return replaced;
        }

        private static Mesh BuildFittedMeshUniform(Mesh objMesh, Mesh original)
        {
            Bounds originalBounds = original.bounds;

            float origMax = Mathf.Max(
                originalBounds.size.x,
                Mathf.Max(originalBounds.size.y, originalBounds.size.z));

            Bounds objBounds = objMesh.bounds;

            float objMax = Mathf.Max(
                objBounds.size.x,
                Mathf.Max(objBounds.size.y, objBounds.size.z));

            float scale = origMax / Mathf.Max(0.001f, objMax);

            Mesh fitted = UnityEngine.Object.Instantiate(objMesh);
            fitted.name = "WVC_Salvia_Obj_Uniform_" + original.name;

            Vector3[] verts = fitted.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = Vector3.Scale(verts[i], new Vector3(scale, scale, scale))
                    + originalBounds.center;
            }

            fitted.vertices = verts;
            fitted.RecalculateBounds();

            return fitted;
        }

        private static Mesh BuildFittedMesh(Mesh objMesh, Mesh original)
        {
            Bounds originalBounds = original.bounds;

            float origX = Mathf.Max(0.001f, originalBounds.size.x);
            float origY = Mathf.Max(0.001f, originalBounds.size.y);
            float origZ = Mathf.Max(0.001f, originalBounds.size.z);

            Bounds objBounds = objMesh.bounds;

            float scaleX = origX / Mathf.Max(0.001f, objBounds.size.x);
            float scaleY = origY / Mathf.Max(0.001f, objBounds.size.y);
            float scaleZ = origZ / Mathf.Max(0.001f, objBounds.size.z);

            Mesh fitted = UnityEngine.Object.Instantiate(objMesh);
            fitted.name = "WVC_Salvia_Obj_" + original.name;

            Vector3[] verts = fitted.vertices;

            for (int i = 0; i < verts.Length; i++)
                verts[i] = Vector3.Scale(verts[i], new Vector3(scaleX, scaleY, scaleZ));

            fitted.vertices = verts;
            fitted.RecalculateBounds();

            return fitted;
        }
    }
}
