using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class MdmaObjVisual
    {
        private const string PreferredResourceName =
            "wvc.mdma.obj";

        private static GameObject _visual;
        private static Mesh _mesh;
        private static Material _material;
        private static bool _failed;

        public static GameObject GetOrCreate()
        {
            if (_visual != null)
                return _visual;

            if (_failed)
                return null;

            Mesh mesh = LoadMesh();

            if (mesh == null)
            {
                _failed = true;
                return null;
            }

            GameObject root =
                new GameObject("WVC_MDMA_ObjVisual");

            MeshFilter filter =
                root.AddComponent<MeshFilter>();

            MeshRenderer renderer =
                root.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;
            renderer.sharedMaterial = GetMaterial();

            root.transform.position =
                new Vector3(0f, -20000f, 0f);

            root.transform.rotation =
                Quaternion.identity;

            root.transform.localScale =
                Vector3.one;

            UnityEngine.Object.DontDestroyOnLoad(root);

            _visual = root;

            MelonLogger.Msg(
                "[WVC MDMA] Embedded OBJ loaded: " +
                mesh.vertexCount + " verts, " +
                (mesh.triangles.Length / 3) + " tris."
            );

            return _visual;
        }

        private static Mesh LoadMesh()
        {
            if (_mesh != null)
                return _mesh;

            try
            {
                Assembly assembly =
                    Assembly.GetExecutingAssembly();

                string resolved =
                    ResolveResourceName(assembly);

                if (resolved == null)
                {
                    MelonLogger.Error(
                        "[WVC MDMA] No embedded .obj found."
                    );

                    foreach (string name in
                        assembly.GetManifestResourceNames())
                    {
                        MelonLogger.Msg("[WVC Resource] " + name);
                    }

                    return null;
                }

                MelonLogger.Msg(
                    "[WVC MDMA] Using resource: " + resolved
                );

                using (Stream stream =
                    assembly.GetManifestResourceStream(resolved))
                {
                    if (stream == null)
                        return null;

                    using (StreamReader reader =
                        new StreamReader(stream))
                    {
                        List<string> lines = new List<string>();
                        string line;

                        while ((line = reader.ReadLine()) != null)
                            lines.Add(line);

                        _mesh = ParseObj(lines.ToArray());
                        _mesh.name = "WVC_MDMA_CustomMesh";
                        return _mesh;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(
                    "[WVC MDMA] Failed to load embedded OBJ: " + ex
                );

                return null;
            }
        }

        private static string ResolveResourceName(
            Assembly assembly)
        {
            string[] names =
                assembly.GetManifestResourceNames();

            foreach (string name in names)
            {
                if (name.EndsWith(
                        PreferredResourceName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            foreach (string name in names)
            {
                if (name.EndsWith(
                        "mdma.obj",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            foreach (string name in names)
            {
                if (name.EndsWith(
                        ".obj",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }

            return null;
        }

        private static Material GetMaterial()
        {
            if (_material != null)
                return _material;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            _material = new Material(shader)
            {
                name = "WVC_MDMA_Obj_Material"
            };

            Color color =
                new Color(0.95f, 0.18f, 0.42f, 1f);

            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", color);

            if (_material.HasProperty("_Color"))
                _material.SetColor("_Color", color);

            if (_material.HasProperty("_Smoothness"))
                _material.SetFloat("_Smoothness", 0.28f);

            if (_material.HasProperty("_Glossiness"))
                _material.SetFloat("_Glossiness", 0.28f);

            if (_material.HasProperty("_Metallic"))
                _material.SetFloat("_Metallic", 0f);

            if (_material.HasProperty("_Cull"))
                _material.SetFloat("_Cull", 0f);

            return _material;
        }

        private static Mesh ParseObj(string[] lines)
        {
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<Vector3> outPos = new List<Vector3>();
            List<Vector3> outNorm = new List<Vector3>();
            List<Vector2> outUv = new List<Vector2>();
            List<int> triangles = new List<int>();

            CultureInfo culture = CultureInfo.InvariantCulture;

            foreach (string raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw) ||
                    raw.StartsWith("#"))
                {
                    continue;
                }

                string[] parts = raw.Trim().Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                );

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
                            parts.Length > 2
                                ? float.Parse(parts[2], culture)
                                : 0f
                        ));
                        break;

                    case "f":
                        AddFace(
                            parts,
                            positions,
                            normals,
                            uvs,
                            outPos,
                            outNorm,
                            outUv,
                            triangles
                        );
                        break;
                }
            }

            if (outPos.Count == 0)
            {
                throw new InvalidOperationException(
                    "OBJ contains no usable vertices."
                );
            }

            Bounds sourceBounds =
                new Bounds(outPos[0], Vector3.zero);

            for (int i = 1; i < outPos.Count; i++)
                sourceBounds.Encapsulate(outPos[i]);

            Vector3 offset = sourceBounds.center;

            for (int i = 0; i < outPos.Count; i++)
                outPos[i] -= offset;

            // Shrink the imported mesh. 1.0 = original. Try 0.15–0.35.
            const float ModelScale = 0.45f;

            for (int i = 0; i < outPos.Count; i++)
                outPos[i] *= ModelScale;

            MelonLogger.Msg(
                "[WVC MDMA] OBJ recentered. Old center=" +
                offset +
                ", size=" +
                sourceBounds.size +
                ", applied scale=" +
                ModelScale
            );
            MelonLogger.Msg(
                "[WVC MDMA] OBJ recentered. Old center=" +
                offset +
                ", size=" +
                sourceBounds.size
            );

            Mesh mesh = new Mesh();
            mesh.name = "WVC_MDMA_CustomMesh";

            mesh.indexFormat =
                outPos.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices = outPos.ToArray();

            if (outUv.Count == outPos.Count)
                mesh.uv = outUv.ToArray();

            mesh.triangles = triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddFace(
            string[] parts,
            List<Vector3> positions,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Vector3> outPos,
            List<Vector3> outNorm,
            List<Vector2> outUv,
            List<int> triangles)
        {
            List<int> face = new List<int>();

            for (int i = 1; i < parts.Length; i++)
            {
                string[] idx = parts[i].Split('/');

                int v = ParseIndex(idx[0], positions.Count);

                int vt = idx.Length > 1 && idx[1].Length > 0
                    ? ParseIndex(idx[1], uvs.Count)
                    : -1;

                int vn = idx.Length > 2 && idx[2].Length > 0
                    ? ParseIndex(idx[2], normals.Count)
                    : -1;

                outPos.Add(positions[v]);
                outUv.Add(vt >= 0 ? uvs[vt] : Vector2.zero);
                outNorm.Add(vn >= 0 ? normals[vn] : Vector3.up);

                face.Add(outPos.Count - 1);
            }

            for (int i = 1; i < face.Count - 1; i++)
            {
                triangles.Add(face[0]);
                triangles.Add(face[i]);
                triangles.Add(face[i + 1]);
            }
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