using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomNPCExample.Products.Edibles
{
    /// <summary>
    /// Creates a single-piece procedural gummy-bear mesh.
    /// </summary>
    public static class GummyVisual
    {
        private static GameObject _source;
        private static Material _material;
        private static Mesh _mesh;

        public static GameObject GetOrCreate()
        {
            if (_source != null)
                return _source;

            _material = CreateGummyMaterial();
            _mesh = CreateGummyBearMesh();

            GameObject root =
                new GameObject("WVC_THC_GummyBear");

            MeshFilter filter =
                root.AddComponent<MeshFilter>();

            MeshRenderer renderer =
                root.AddComponent<MeshRenderer>();

            filter.sharedMesh = _mesh;
            renderer.sharedMaterial = _material;

            root.transform.position =
                new Vector3(0f, -20000f, 0f);

            root.transform.rotation =
                Quaternion.identity;

            root.transform.localScale =
                Vector3.one;

            root.SetActive(true);

            UnityEngine.Object.DontDestroyOnLoad(root);
            UnityEngine.Object.DontDestroyOnLoad(_mesh);
            UnityEngine.Object.DontDestroyOnLoad(_material);

            _source = root;

            MelonLoader.MelonLogger.Msg(
                "[WVC Gummies] Single-piece gummy-bear visual created."
            );

            return _source;
        }

        private static Mesh CreateGummyBearMesh()
        {
            /*
             * One continuous bear silhouette in the XY plane.
             *
             * The arms, ears and legs are part of the outline, so they
             * cannot become detached from the body.
             */
            Vector2[] outline =
            {
                // Top of head
                new Vector2( 0.000f,  0.029f),
                new Vector2(-0.006f,  0.029f),

                // Left ear
                new Vector2(-0.008f,  0.033f),
                new Vector2(-0.013f,  0.035f),
                new Vector2(-0.017f,  0.033f),
                new Vector2(-0.019f,  0.029f),
                new Vector2(-0.018f,  0.025f),
                new Vector2(-0.015f,  0.022f),

                // Left side of head
                new Vector2(-0.016f,  0.017f),
                new Vector2(-0.014f,  0.013f),

                // Left arm
                new Vector2(-0.018f,  0.010f),
                new Vector2(-0.021f,  0.005f),
                new Vector2(-0.020f,  0.001f),
                new Vector2(-0.017f, -0.001f),
                new Vector2(-0.014f,  0.001f),

                // Left side of body
                new Vector2(-0.012f, -0.006f),

                // Left leg and foot
                new Vector2(-0.014f, -0.012f),
                new Vector2(-0.014f, -0.018f),
                new Vector2(-0.011f, -0.021f),
                new Vector2(-0.006f, -0.021f),
                new Vector2(-0.003f, -0.016f),

                // Between legs
                new Vector2( 0.000f, -0.013f),

                // Right leg and foot
                new Vector2( 0.003f, -0.016f),
                new Vector2( 0.006f, -0.021f),
                new Vector2( 0.011f, -0.021f),
                new Vector2( 0.014f, -0.018f),
                new Vector2( 0.014f, -0.012f),

                // Right side of body
                new Vector2( 0.012f, -0.006f),

                // Right arm
                new Vector2( 0.014f,  0.001f),
                new Vector2( 0.017f, -0.001f),
                new Vector2( 0.020f,  0.001f),
                new Vector2( 0.021f,  0.005f),
                new Vector2( 0.018f,  0.010f),

                // Right side of head
                new Vector2( 0.014f,  0.013f),
                new Vector2( 0.016f,  0.017f),
                new Vector2( 0.015f,  0.022f),

                // Right ear
                new Vector2( 0.018f,  0.025f),
                new Vector2( 0.019f,  0.029f),
                new Vector2( 0.017f,  0.033f),
                new Vector2( 0.013f,  0.035f),
                new Vector2( 0.008f,  0.033f),
                new Vector2( 0.006f,  0.029f)
            };

            if (SignedArea(outline) < 0f)
                Array.Reverse(outline);

            List<int> capTriangles =
                TriangulatePolygon(outline);

            int count = outline.Length;

            /*
             * Four rings create slightly beveled edges:
             *
             * 0 = front face
             * 1 = front outer edge
             * 2 = back outer edge
             * 3 = back face
             */
            const int ringCount = 4;

            float[] depths =
            {
                 0.0050f,
                 0.0032f,
                -0.0032f,
                -0.0050f
            };

            float[] scales =
            {
                0.93f,
                1.00f,
                1.00f,
                0.93f
            };

            Vector2 center =
                CalculateCenter(outline);

            Vector3[] vertices =
                new Vector3[count * ringCount];

            for (int ring = 0; ring < ringCount; ring++)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 offset =
                        outline[i] - center;

                    Vector2 point =
                        center + offset * scales[ring];

                    vertices[ring * count + i] =
                        new Vector3(
                            point.x,
                            point.y,
                            depths[ring]
                        );
                }
            }

            List<int> triangles =
                new List<int>();

            // Front face
            for (int i = 0; i < capTriangles.Count; i += 3)
            {
                triangles.Add(capTriangles[i]);
                triangles.Add(capTriangles[i + 1]);
                triangles.Add(capTriangles[i + 2]);
            }

            // Back face, with reversed winding
            int backOffset =
                (ringCount - 1) * count;

            for (int i = 0; i < capTriangles.Count; i += 3)
            {
                triangles.Add(
                    backOffset + capTriangles[i]
                );

                triangles.Add(
                    backOffset + capTriangles[i + 2]
                );

                triangles.Add(
                    backOffset + capTriangles[i + 1]
                );
            }

            // Connect all four rings
            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                int currentOffset =
                    ring * count;

                int nextOffset =
                    (ring + 1) * count;

                for (int i = 0; i < count; i++)
                {
                    int next =
                        (i + 1) % count;

                    triangles.Add(currentOffset + i);
                    triangles.Add(nextOffset + i);
                    triangles.Add(currentOffset + next);

                    triangles.Add(currentOffset + next);
                    triangles.Add(nextOffset + i);
                    triangles.Add(nextOffset + next);
                }
            }

            Mesh mesh =
                new Mesh();

            mesh.name =
                "WVC_THC_GummyBear_Mesh";

            mesh.vertices =
                vertices;

            mesh.triangles =
                triangles.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Material CreateGummyMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit"
                )
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                throw new InvalidOperationException(
                    "No compatible shader found for the gummy."
                );
            }

            Material material =
                new Material(shader);

            material.name =
                "WVC_THC_GummyRed_Material";

            /*
             * Keep it opaque for now. Transparent materials can produce
             * sorting issues on procedural models.
             */
            Color gummyRed =
                new Color(
                    0.92f,
                    0.035f,
                    0.018f,
                    1f
                );

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    gummyRed
                );
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    gummyRed
                );
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat(
                    "_Smoothness",
                    0.88f
                );
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat(
                    "_Glossiness",
                    0.88f
                );
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat(
                    "_Metallic",
                    0f
                );
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor(
                    "_EmissionColor",
                    new Color(
                        0.035f,
                        0.001f,
                        0.001f,
                        1f
                    )
                );

                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        private static Vector2 CalculateCenter(
            Vector2[] points)
        {
            Vector2 result =
                Vector2.zero;

            for (int i = 0; i < points.Length; i++)
                result += points[i];

            return result / points.Length;
        }

        private static List<int> TriangulatePolygon(
            Vector2[] points)
        {
            List<int> result =
                new List<int>();

            List<int> remaining =
                new List<int>();

            for (int i = 0; i < points.Length; i++)
                remaining.Add(i);

            int safety = 0;

            while (remaining.Count > 3 &&
                   safety++ < 512)
            {
                bool foundEar = false;

                for (int i = 0; i < remaining.Count; i++)
                {
                    int previousIndex =
                        remaining[
                            (i - 1 + remaining.Count) %
                            remaining.Count
                        ];

                    int currentIndex =
                        remaining[i];

                    int nextIndex =
                        remaining[
                            (i + 1) %
                            remaining.Count
                        ];

                    Vector2 a =
                        points[previousIndex];

                    Vector2 b =
                        points[currentIndex];

                    Vector2 c =
                        points[nextIndex];

                    if (Cross2D(a, b, c) <= 0.0000001f)
                        continue;

                    bool containsPoint = false;

                    foreach (int testIndex in remaining)
                    {
                        if (testIndex == previousIndex ||
                            testIndex == currentIndex ||
                            testIndex == nextIndex)
                        {
                            continue;
                        }

                        if (PointInTriangle(
                            points[testIndex],
                            a,
                            b,
                            c))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                        continue;

                    result.Add(previousIndex);
                    result.Add(currentIndex);
                    result.Add(nextIndex);

                    remaining.RemoveAt(i);

                    foundEar = true;
                    break;
                }

                if (!foundEar)
                    break;
            }

            if (remaining.Count == 3)
            {
                result.Add(remaining[0]);
                result.Add(remaining[1]);
                result.Add(remaining[2]);
            }

            return result;
        }

        private static float SignedArea(
            Vector2[] points)
        {
            float area = 0f;

            for (int i = 0; i < points.Length; i++)
            {
                int next =
                    (i + 1) % points.Length;

                area +=
                    points[i].x * points[next].y -
                    points[next].x * points[i].y;
            }

            return area * 0.5f;
        }

        private static float Cross2D(
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            return
                (b.x - a.x) * (c.y - a.y) -
                (b.y - a.y) * (c.x - a.x);
        }

        private static bool PointInTriangle(
            Vector2 point,
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            float d1 =
                Cross2D(a, b, point);

            float d2 =
                Cross2D(b, c, point);

            float d3 =
                Cross2D(c, a, point);

            bool hasNegative =
                d1 < 0f ||
                d2 < 0f ||
                d3 < 0f;

            bool hasPositive =
                d1 > 0f ||
                d2 > 0f ||
                d3 > 0f;

            return !(hasNegative && hasPositive);
        }
    }
}