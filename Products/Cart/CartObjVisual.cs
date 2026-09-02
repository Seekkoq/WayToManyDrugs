using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class CartObjVisual
    {
        private const string PreferredResourceName = "cart.obj";

        private static GameObject _visual;
        private static ObjResult _objResult;
        private static bool _failed;

        private sealed class ObjResult
        {
            public Mesh Mesh;
            public List<string> MaterialNames;
        }

        public static GameObject GetOrCreate()
        {
            if (_visual != null)
                return _visual;

            if (_failed)
                return null;

            ObjResult result = LoadObj();

            if (result == null || result.Mesh == null)
            {
                _failed = true;
                return null;
            }

            GameObject root = new GameObject("WVC_Cart_ObjVisual");

            MeshFilter filter = root.AddComponent<MeshFilter>();
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();

            filter.sharedMesh = result.Mesh;

            Material[] materials = new Material[result.MaterialNames.Count];
            for (int i = 0; i < result.MaterialNames.Count; i++)
            {
                materials[i] = GetMaterialFor(result.MaterialNames[i]);
            }

            renderer.sharedMaterials = materials;

            root.transform.position = new Vector3(0f, -20000f, 0f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            UnityEngine.Object.DontDestroyOnLoad(root);

            _visual = root;

            MelonLogger.Msg(
                "[WVC Cart] Embedded OBJ loaded: " +
                result.Mesh.vertexCount + " verts, " +
                result.MaterialNames.Count + " material groups."
            );

            return _visual;
        }

        private static ObjResult LoadObj()
        {
            if (_objResult != null)
                return _objResult;

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resolved = ResolveResourceName(assembly);

                if (resolved == null)
                {
                    MelonLogger.Error("[WVC Cart] No embedded resource ending in 'cart.obj' found.");
                    foreach (string name in assembly.GetManifestResourceNames())
                    {
                        MelonLogger.Msg("[WVC Resource] " + name);
                    }
                    return data_dummy();
                }

                MelonLogger.Msg("[WVC Cart] Using resource: " + resolved);

                using (Stream stream = assembly.GetManifestResourceStream(resolved))
                {
                    if (stream == null)
                        return null;

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        List<string> lines = new List<string>();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                            lines.Add(line);

                        _objResult = ParseObj(lines.ToArray());
                        _objResult.Mesh.name = "WVC_Cart_CustomMesh";
                        return _objResult;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[WVC Cart] Failed to load embedded OBJ: " + ex);
                return null;
            }
        }

        private static ObjResult data_dummy() => null;

        private static string ResolveResourceName(Assembly assembly)
        {
            string[] names = assembly.GetManifestResourceNames();
            foreach (string name in names)
            {
                if (name.EndsWith(PreferredResourceName, StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }
            return null;
        }

        // ============================================================
        // Material Management (White Body/Tip, Ultra-Clear Glass, Liquid Oil)
        // ============================================================

        private static readonly Dictionary<string, Material> MaterialCache =
            new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

        private static bool IsOilMaterial(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;
            string lower = materialName.ToLowerInvariant();
            return lower.Contains("oil") || lower.Contains("liquid") ||
                   lower.Contains("thc") || lower.Contains("juice") || lower.Contains("distillate");
        }

        private static bool IsGlassMaterial(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName)) return false;
            string lower = materialName.ToLowerInvariant();
            return lower.Contains("glass") || lower.Contains("clear") ||
                   lower.Contains("containor") || lower.Contains("container");
        }

        private static Material GetMaterialFor(string materialName)
        {
            if (MaterialCache.TryGetValue(materialName, out Material cached))
                return cached;

            string lower = materialName.ToLowerInvariant();

            // Check strings first, then configure settings
            bool isGlass = lower.Contains("glass");
            bool isOil = lower.Contains("oil") || lower.Contains("liquid");

            Color color;
            float smoothness = 0.5f;

            if (isGlass)
            {
                // Force fully clear
                color = new Color(0.9f, 0.95f, 1.0f, 0.05f);
                smoothness = 0.95f;
            }
            else if (isOil)
            {
                // Amber Liquid
                color = new Color(1.0f, 0.6f, 0.1f, 0.8f);
                smoothness = 0.8f;
            }
            else
            {
                // White Body/Tip
                color = Color.white;
                smoothness = 0.3f;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);

            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);

            if (isGlass || isOil)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = isOil ? 3000 : 3100;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            MaterialCache[materialName] = mat;
            return mat;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material == null) return;

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);

            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);

            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            material.DisableKeyword("_ALPHATEST_ON");

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ============================================================
        // OBJ Parsing with Automatic Oil Inset
        // ============================================================

        private static ObjResult ParseObj(string[] lines)
        {
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<Vector3> outPos = new List<Vector3>();
            List<Vector3> outNorm = new List<Vector3>();
            List<Vector2> outUv = new List<Vector2>();

            List<List<int>> groups = new List<List<int>>();
            List<string> groupNames = new List<string>();
            List<int> currentGroup = null;

            HashSet<int> oilVertexIndices = new HashSet<int>();
            bool currentGroupIsOil = false;

            CultureInfo culture = CultureInfo.InvariantCulture;

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#"))
                    continue;

                string[] parts = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "v":
                        positions.Add(new Vector3(
                            float.Parse(parts[1], culture),
                            float.Parse(parts[2], culture),
                            float.Parse(parts[3], culture)
                        ));
                        break;

                    case "vn":
                        normals.Add(new Vector3(
                            float.Parse(parts[1], culture),
                            float.Parse(parts[2], culture),
                            float.Parse(parts[3], culture)
                        ));
                        break;

                    case "vt":
                        uvs.Add(new Vector2(
                            float.Parse(parts[1], culture),
                            parts.Length > 2 ? float.Parse(parts[2], culture) : 0f
                        ));
                        break;

                    case "usemtl":
                        string materialName = parts.Length > 1 ? parts[1] : "default";
                        currentGroup = new List<int>();
                        groups.Add(currentGroup);
                        groupNames.Add(materialName);
                        currentGroupIsOil = IsOilMaterial(materialName);
                        break;

                    case "f":
                        if (currentGroup == null)
                        {
                            currentGroup = new List<int>();
                            groups.Add(currentGroup);
                            groupNames.Add("default");
                            currentGroupIsOil = false;
                        }

                        AddFace(
                            parts,
                            positions,
                            normals,
                            uvs,
                            outPos,
                            outNorm,
                            outUv,
                            currentGroup,
                            currentGroupIsOil,
                            oilVertexIndices
                        );
                        break;
                }
            }

            if (outPos.Count == 0 || groups.Count == 0)
                throw new InvalidOperationException("Cart OBJ has no usable geometry.");

            // RECENTER MESH AROUND ORIGIN
            Bounds bounds = new Bounds(outPos[0], Vector3.zero);
            for (int i = 1; i < outPos.Count; i++)
                bounds.Encapsulate(outPos[i]);

            Vector3 center = bounds.center;
            for (int i = 0; i < outPos.Count; i++)
                outPos[i] -= center;

            // AUTOMATICALLY SHRINK THE OIL MESH ON X/Z TO FIT INSIDE GLASS
            if (oilVertexIndices.Count > 0)
            {
                int firstOil = GetFirstOilVertex(oilVertexIndices);
                Bounds oilBounds = new Bounds(outPos[firstOil], Vector3.zero);
                foreach (int idx in oilVertexIndices)
                    oilBounds.Encapsulate(outPos[idx]);

                Vector3 oilCenter = oilBounds.center;
                const float OilScale = 0.90f; // Shrinks oil to 90% radius

                foreach (int idx in oilVertexIndices)
                {
                    Vector3 pt = outPos[idx];
                    pt.x = oilCenter.x + ((pt.x - oilCenter.x) * OilScale);
                    pt.z = oilCenter.z + ((pt.z - oilCenter.z) * OilScale);
                    outPos[idx] = pt;
                }

                MelonLogger.Msg("[WVC Cart] Inset oil mesh. Vertices=" + oilVertexIndices.Count);
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = outPos.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices = outPos.ToArray();

            if (outUv.Count == outPos.Count)
                mesh.uv = outUv.ToArray();

            mesh.subMeshCount = groups.Count;
            for (int i = 0; i < groups.Count; i++)
            {
                mesh.SetTriangles(groups[i].ToArray(), i);
            }

            bool hasEmbeddedNormals = normals.Count > 0 && outNorm.Count == outPos.Count;
            if (hasEmbeddedNormals)
            {
                mesh.normals = outNorm.ToArray();
            }
            else
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            MelonLogger.Msg("[WVC Cart] OBJ groups: " + string.Join(", ", groupNames.ToArray()));

            return new ObjResult
            {
                Mesh = mesh,
                MaterialNames = groupNames
            };
        }

        private static void AddFace(
            string[] parts,
            List<Vector3> positions,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Vector3> outPos,
            List<Vector3> outNorm,
            List<Vector2> outUv,
            List<int> triangles,
            bool isOil,
            HashSet<int> oilVertexIndices)
        {
            List<int> face = new List<int>();

            for (int i = 1; i < parts.Length; i++)
            {
                string[] idx = parts[i].Split('/');

                int v = ParseIndex(idx[0], positions.Count);
                int vt = idx.Length > 1 && idx[1].Length > 0 ? ParseIndex(idx[1], uvs.Count) : -1;
                int vn = idx.Length > 2 && idx[2].Length > 0 ? ParseIndex(idx[2], normals.Count) : -1;

                outPos.Add(positions[v]);
                outUv.Add(vt >= 0 ? uvs[vt] : Vector2.zero);
                outNorm.Add(vn >= 0 ? normals[vn] : Vector3.up);

                int newVertexIndex = outPos.Count - 1;
                face.Add(newVertexIndex);

                if (isOil)
                {
                    oilVertexIndices.Add(newVertexIndex);
                }
            }

            for (int i = 1; i < face.Count - 1; i++)
            {
                triangles.Add(face[0]);
                triangles.Add(face[i]);
                triangles.Add(face[i + 1]);
            }
        }

        private static int GetFirstOilVertex(HashSet<int> indices)
        {
            foreach (int idx in indices) return idx;
            return 0;
        }

        private static int ParseIndex(string token, int count)
        {
            int index = int.Parse(token, CultureInfo.InvariantCulture);
            if (index < 0)
                return count + index;
            return index - 1;
        }
    }
}