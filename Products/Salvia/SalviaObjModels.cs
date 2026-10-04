using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public class SalviaObjModel
    {
        public Mesh Mesh;
        public Color[] SubMeshColors;

        private Material[] _materials;

        public Material[] GetMaterials()
        {
            if (_materials != null && _materials.Length == SubMeshColors.Length)
                return _materials;

            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            _materials = new Material[SubMeshColors.Length];

            for (int i = 0; i < SubMeshColors.Length; i++)
            {
                Material material = new Material(shader)
                {
                    name = "WVC_Salvia_Obj_Material_" + i
                };

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", SubMeshColors[i]);

                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", SubMeshColors[i]);

                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.2f);

                if (material.HasProperty("_Glossiness"))
                    material.SetFloat("_Glossiness", 0.2f);

                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 0f);

                if (material.HasProperty("_Cull"))
                    material.SetFloat("_Cull", 0f);

                _materials[i] = material;
            }

            return _materials;
        }
    }

    public static class SalviaObjModels
    {
        private const string PlantResource =
            "salvia_plant.obj";

        private const string PlantMtlResource =
            "salvia_plant.mtl";

        private const string HarvestableResource =
            "salvia_plant_harvestable.obj";

        private const string HarvestableMtlResource =
            "salvia_plant_harvestable.mtl";

        private static SalviaObjModel _plant;
        private static SalviaObjModel _harvestable;
        private static bool _plantFailed;
        private static bool _harvestableFailed;

        public static SalviaObjModel Plant
        {
            get
            {
                if (_plant == null && !_plantFailed)
                {
                    _plant = Load(PlantResource, PlantMtlResource);

                    if (_plant == null)
                        _plantFailed = true;
                }

                return _plant;
            }
        }

        public static SalviaObjModel Harvestable
        {
            get
            {
                if (_harvestable == null && !_harvestableFailed)
                {
                    _harvestable = Load(HarvestableResource, HarvestableMtlResource);

                    if (_harvestable == null)
                        _harvestableFailed = true;
                }

                return _harvestable;
            }
        }

        private static SalviaObjModel Load(string objResource, string mtlResource)
        {
            try
            {
                Assembly assembly =
                    Assembly.GetExecutingAssembly();

                string objName = ResolveResource(assembly, objResource);
                string mtlName = ResolveResource(assembly, mtlResource);

                if (objName == null)
                {

                    return null;
                }

                Dictionary<string, Color> mtlColors = mtlName != null
                    ? ParseMtl(ReadAllLines(assembly, mtlName))
                    : new Dictionary<string, Color>();

                return ParseObj(ReadAllLines(assembly, objName), mtlColors);
            }
            catch (Exception)
            {

                return null;
            }
        }

        private static string[] ReadAllLines(Assembly assembly, string name)
        {
            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    return new string[0];

                using (StreamReader reader = new StreamReader(stream))
                {
                    List<string> lines = new List<string>();
                    string line;

                    while ((line = reader.ReadLine()) != null)
                        lines.Add(line);

                    return lines.ToArray();
                }
            }
        }

        private static string ResolveResource(Assembly assembly, string fileName)
        {
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            return null;
        }

        private static Dictionary<string, Color> ParseMtl(string[] lines)
        {
            Dictionary<string, Color> colors = new Dictionary<string, Color>();
            string current = null;

            foreach (string raw in lines)
            {
                string line = raw.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts[0] == "newmtl" && parts.Length >= 2)
                {
                    current = parts[1];
                }
                else if (parts[0] == "Kd" && parts.Length >= 4 && current != null)
                {
                    Color color = new Color(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture),
                        1f);

                    colors[current] = Color.Lerp(
                        color,
                        color * 1.6f + new Color(0.06f, 0.06f, 0.06f),
                        0.5f);
                }
            }

            return colors;
        }

        private static SalviaObjModel ParseObj(
            string[] lines,
            Dictionary<string, Color> mtlColors)
        {
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> outPos = new List<Vector3>();
            List<Vector3> outNorm = new List<Vector3>();
            List<Vector2> outUv = new List<Vector2>();
            List<string> materialOrder = new List<string>();
            List<List<int>> trianglesPerMaterial = new List<List<int>>();

            int currentMaterial = -1;

            foreach (string raw in lines)
            {
                string line = raw.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts[0] == "v" && parts.Length >= 4)
                {
                    positions.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "vt" && parts.Length >= 3)
                {
                    uvs.Add(new Vector2(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "vn" && parts.Length >= 4)
                {
                    normals.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                }
                else if (parts[0] == "usemtl" && parts.Length >= 2)
                {
                    string name = parts[1];

                    int index = materialOrder.IndexOf(name);

                    if (index < 0)
                    {
                        index = materialOrder.Count;
                        materialOrder.Add(name);
                        trianglesPerMaterial.Add(new List<int>());
                    }

                    currentMaterial = index;
                }
                else if (parts[0] == "f" && parts.Length >= 4)
                {
                    if (currentMaterial < 0)
                    {
                        currentMaterial = 0;
                        materialOrder.Add("default");
                        trianglesPerMaterial.Add(new List<int>());
                    }

                    AddFace(
                        parts,
                        positions,
                        normals,
                        uvs,
                        outPos,
                        outNorm,
                        outUv,
                        trianglesPerMaterial[currentMaterial]);
                }
            }

            if (outPos.Count == 0)
                throw new InvalidOperationException("OBJ contains no usable vertices.");

            Bounds sourceBounds = new Bounds(outPos[0], Vector3.zero);

            for (int i = 1; i < outPos.Count; i++)
                sourceBounds.Encapsulate(outPos[i]);

            Vector3 offset = sourceBounds.center;

            for (int i = 0; i < outPos.Count; i++)
                outPos[i] -= offset;

            Mesh mesh = BuildMesh(outPos, outNorm, outUv, trianglesPerMaterial);

            Color[] colors = new Color[materialOrder.Count];

            for (int i = 0; i < materialOrder.Count; i++)
            {
                Color color;

                if (!mtlColors.TryGetValue(materialOrder[i], out color))
                    color = new Color(0.30f, 0.52f, 0.22f, 1f);

                colors[i] = color;
            }

            return new SalviaObjModel
            {
                Mesh = mesh,
                SubMeshColors = colors
            };
        }

        private static Mesh BuildMesh(
            List<Vector3> outPos,
            List<Vector3> outNorm,
            List<Vector2> outUv,
            List<List<int>> trianglesPerMaterial)
        {
            Mesh mesh = new Mesh();
            mesh.name = "WVC_Salvia_CustomMesh";

            mesh.indexFormat =
                outPos.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices = outPos.ToArray();

            if (outNorm.Count == outPos.Count)
                mesh.normals = outNorm.ToArray();

            if (outUv.Count == outPos.Count)
                mesh.uv = outUv.ToArray();

            if (trianglesPerMaterial.Count == 0)
            {
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                return mesh;
            }

            mesh.subMeshCount = trianglesPerMaterial.Count;

            for (int i = 0; i < trianglesPerMaterial.Count; i++)
                mesh.SetTriangles(trianglesPerMaterial[i].ToArray(), i);

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