using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class SalviaObjVisual
    {
        private const string PreferredResourceName =
            "salvia_plant.obj";

        private static GameObject _visual;

        public static Mesh GetMesh()
        {
            SalviaObjModel model = SalviaObjModels.Plant;

            return model != null ? model.Mesh : null;
        }

        public static Material GetPlantMaterial()
        {
            SalviaObjModel model = SalviaObjModels.Plant;

            if (model == null)
                return null;

            Material[] materials = model.GetMaterials();

            return materials != null && materials.Length > 0
                ? materials[0]
                : null;
        }

        public static GameObject GetOrCreate()
        {
            if (_visual != null)
                return _visual;

            SalviaObjModel model = SalviaObjModels.Plant;

            if (model == null || model.Mesh == null)
                return null;

            GameObject root =
                new GameObject("WVC_Salvia_ObjVisual");

            MeshFilter filter =
                root.AddComponent<MeshFilter>();

            MeshRenderer renderer =
                root.AddComponent<MeshRenderer>();

            filter.sharedMesh = model.Mesh;
            renderer.sharedMaterials = model.GetMaterials();

            root.transform.position =
                new Vector3(0f, -20000f, 0f);

            root.transform.rotation =
                Quaternion.identity;

            root.transform.localScale =
                Vector3.one;

            UnityEngine.Object.DontDestroyOnLoad(root);

            _visual = root;

            return root;
        }

    }
}
