using System.Collections;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class CartVapor
    {
        private static bool _playing;

        // Materials & Textures
        private static Material _vaporMaterial;
        private static Texture2D _smokeTexture;

        private static Material _ringMaterial;
        private static Texture2D _ringTexture;

        // ============================================================
        // MOUTH POSITION & SMOKE TUNING
        // ============================================================
        private const float MouthForward = 0.22f;
        private const float MouthDown = -0.12f;
        private const float MouthRight = 0.00f;

        private const float CloudOpacity = 0.40f;
        private const float RingOpacity = 0.85f;  // Brighter for the solid O

        public static void PlayVapor()
        {
            if (_playing) return;
            MelonCoroutines.Start(ExhaleTrickRoutine());
        }

        private static IEnumerator ExhaleTrickRoutine()
        {
            _playing = true;

            Camera camera = Camera.main;
            if (camera == null)
            {
                _playing = false;
                yield break;
            }

            Transform camT = camera.transform;
            Vector3 exhaleDir = camT.forward;
            Vector3 exhaleUp = camT.up;
            Vector3 exhaleRight = camT.right;

            // 1. Blow a few initial cloudy puffs
            for (int i = 0; i < 5; i++)
            {
                SpawnSmokePuff(camT, exhaleDir, exhaleUp, exhaleRight, 0.04f, 0.25f);
                yield return new WaitForSeconds(0.04f);
            }

            // 2. SHOOT THE SOLID O-RING
            SpawnSolidRing(camT, exhaleDir, exhaleUp, exhaleRight);

            // 3. Blow trailing smoke through the middle of the ring
            for (int i = 0; i < 8; i++)
            {
                SpawnSmokePuff(camT, exhaleDir, exhaleUp, exhaleRight, 0.03f, 0.15f);
                yield return new WaitForSeconds(0.06f);
            }

            yield return new WaitForSeconds(1.8f);
            _playing = false;
        }

        // ============================================================
        // THE SOLID "O" RING
        // ============================================================

        private static void SpawnSolidRing(Transform camT, Vector3 forward, Vector3 up, Vector3 right)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "WVC_VapeSmoke_SolidRing";
            StripPhysicsImmediate(ring);

            Vector3 mouthOrigin = camT.position + (forward * MouthForward) + (up * MouthDown) + (right * MouthRight);

            ring.transform.position = mouthOrigin;
            ring.transform.rotation = Quaternion.LookRotation(forward, up);

            // Starts small at the lips
            ring.transform.localScale = Vector3.one * 0.03f;

            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(GetRingMaterial());
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            UnityEngine.Object.DontDestroyOnLoad(ring);

            MelonCoroutines.Start(AnimateSolidRing(ring, mouthOrigin, forward));
        }

        private static IEnumerator AnimateSolidRing(GameObject ring, Vector3 startPos, Vector3 travelDir)
        {
            if (ring == null) yield break;

            Transform t = ring.transform;
            Material mat = ring.GetComponent<MeshRenderer>().sharedMaterial;

            float lifetime = 2.2f;
            float elapsed = 0f;

            // How far the ring travels forward
            float maxDistance = 1.2f;
            // How big the ring gets
            float finalSize = 0.55f;

            while (elapsed < lifetime)
            {
                if (ring == null) yield break;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lifetime);

                // Smooth deceleration curve for forward movement
                float moveProgress = Mathf.Pow(progress, 0.45f);
                t.position = startPos + (travelDir * (moveProgress * maxDistance));

                // Expansion curve (rings grow as they slow down)
                float currentSize = Mathf.Lerp(0.03f, finalSize, Mathf.Pow(progress, 0.6f));
                t.localScale = Vector3.one * currentSize;

                // Always billboard to face the player so the O doesn't vanish if they turn
                if (Camera.main != null)
                {
                    t.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
                }

                // Alpha fade out
                if (mat != null)
                {
                    float alpha;
                    if (progress < 0.1f) alpha = Mathf.Lerp(0f, RingOpacity, progress / 0.1f); // fade in fast
                    else alpha = Mathf.Lerp(RingOpacity, 0f, (progress - 0.1f) / 0.9f);        // fade out slow

                    Color c = new Color(0.98f, 0.99f, 1f, alpha);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                }

                yield return null;
            }

            if (ring != null) UnityEngine.Object.Destroy(ring);
        }

        // ============================================================
        // NORMAL TRAILING SMOKE PUFFS
        // ============================================================

        private static void SpawnSmokePuff(Transform camT, Vector3 forward, Vector3 up, Vector3 right, float startSize, float endSize)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Quad);
            puff.name = "WVC_VapeSmoke_Puff";
            StripPhysicsImmediate(puff);

            Vector3 mouthOrigin = camT.position + (forward * MouthForward) + (up * MouthDown) + (right * MouthRight);

            puff.transform.position = mouthOrigin;
            puff.transform.rotation = Quaternion.LookRotation(forward, up) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            puff.transform.localScale = Vector3.one * startSize;

            MeshRenderer renderer = puff.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(GetVaporMaterial());
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            UnityEngine.Object.DontDestroyOnLoad(puff);
            MelonCoroutines.Start(AnimateSmokePuff(puff, mouthOrigin, forward, up, right, startSize, endSize));
        }

        private static IEnumerator AnimateSmokePuff(GameObject puff, Vector3 startPos, Vector3 shootForward, Vector3 driftUp, Vector3 spreadRight, float startSize, float endSize)
        {
            if (puff == null) yield break;

            Transform t = puff.transform;
            Material mat = puff.GetComponent<MeshRenderer>().sharedMaterial;

            // Shoots faster to "catch up" to and go through the ring
            Vector3 forwardVelocity = shootForward * Random.Range(0.6f, 0.85f);
            Vector3 upwardVelocity = driftUp * Random.Range(0.02f, 0.08f);
            Vector3 sideVelocity = spreadRight * Random.Range(-0.04f, 0.04f);
            Vector3 totalVelocity = forwardVelocity + upwardVelocity + sideVelocity;

            float lifetime = Random.Range(1.2f, 1.8f);
            float elapsed = 0f;
            float spinSpeed = Random.Range(-30f, 30f);

            while (elapsed < lifetime)
            {
                if (puff == null) yield break;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lifetime);

                float moveProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);
                t.position = startPos + (totalVelocity * moveProgress);

                float currentSize = Mathf.Lerp(startSize, endSize, Mathf.Pow(progress, 0.65f));
                t.localScale = Vector3.one * currentSize;

                if (Camera.main != null)
                {
                    t.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up) *
                                 Quaternion.Euler(0, 0, elapsed * spinSpeed);
                }

                if (mat != null)
                {
                    float alphaCurve = progress < 0.15f
                        ? Mathf.Lerp(0f, CloudOpacity, progress / 0.15f)
                        : Mathf.Lerp(CloudOpacity, 0f, (progress - 0.15f) / 0.85f);

                    Color c = new Color(0.96f, 0.98f, 1f, alphaCurve);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                }

                yield return null;
            }

            if (puff != null) UnityEngine.Object.Destroy(puff);
        }

        // ============================================================
        // PROCEDURAL TEXTURES & MATERIALS
        // ============================================================

        private static Material GetVaporMaterial()
        {
            if (_vaporMaterial != null) return _vaporMaterial;
            _vaporMaterial = CreateTransparentMaterial("WVC_ProceduralVapeSmoke_Mat", GetSmokeTexture());
            return _vaporMaterial;
        }

        private static Material GetRingMaterial()
        {
            if (_ringMaterial != null) return _ringMaterial;
            _ringMaterial = CreateTransparentMaterial("WVC_ProceduralVapeRing_Mat", GetRingTexture());
            return _ringMaterial;
        }

        private static Material CreateTransparentMaterial(string matName, Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName };

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);

            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;

            return mat;
        }

        private static Texture2D GetSmokeTexture()
        {
            if (_smokeTexture != null) return _smokeTexture;

            int size = 128;
            _smokeTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "WVC_SoftSmoke_PuffTex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                    float alpha = Mathf.Clamp01(Mathf.Cos(Mathf.Clamp01(dist) * Mathf.PI * 0.5f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(alpha, 1.7f));
                }
            }

            _smokeTexture.SetPixels(pixels);
            _smokeTexture.Apply();
            UnityEngine.Object.DontDestroyOnLoad(_smokeTexture);
            return _smokeTexture;
        }

        private static Texture2D GetRingTexture()
        {
            if (_ringTexture != null) return _ringTexture;

            int size = 256; // Higher resolution for crisp O-ring
            _ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "WVC_SolidRing_Tex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.48f;

            // Where the thickest part of the ring sits (0.0 to 1.0)
            float ringCenter = 0.70f;
            // How thick the ring is
            float ringThickness = 0.18f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;

                    // Calculate how far we are from the center of the "O" line
                    float diff = Mathf.Abs(dist - ringCenter);

                    // Creates a gradient that peaks at ringCenter and fades to 0 at the thickness edges
                    float alpha = Mathf.Clamp01(1f - (diff / ringThickness));

                    // Curve it so the inside/outside edges are soft but the core is solid
                    alpha = Mathf.Pow(alpha, 1.2f);

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _ringTexture.SetPixels(pixels);
            _ringTexture.Apply();
            UnityEngine.Object.DontDestroyOnLoad(_ringTexture);
            return _ringTexture;
        }

        private static void StripPhysicsImmediate(GameObject go)
        {
            if (go == null) return;

            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                c.enabled = false;
                try { UnityEngine.Object.DestroyImmediate(c); }
                catch { try { UnityEngine.Object.Destroy(c); } catch { } }
            }

            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null) continue;
                rb.isKinematic = true;
                rb.detectCollisions = false;
                try { UnityEngine.Object.DestroyImmediate(rb); }
                catch { try { UnityEngine.Object.Destroy(rb); } catch { } }
            }
        }
    }
}