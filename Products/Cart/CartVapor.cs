using System.Collections;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class CartVapor
    {
        private static bool _playing;

        private static Material _vaporMaterial;
        private static Texture2D _smokeTexture;

        private static Material _ringMaterial;
        private static Texture2D _ringTexture;

        private const float MouthForward = 0.22f;
        private const float MouthDown = -0.12f;
        private const float MouthRight = 0.00f;

        private const float CloudOpacity = 0.40f;
        private const float RingOpacity = 0.85f;

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

            for (int i = 0; i < 5; i++)
            {
                SpawnSmokePuff(camT, exhaleDir, exhaleUp, exhaleRight, 0.04f, 0.25f);
                yield return new WaitForSeconds(0.04f);
            }

            SpawnSolidRing(camT, exhaleDir, exhaleUp, exhaleRight);

            for (int i = 0; i < 8; i++)
            {
                SpawnSmokePuff(camT, exhaleDir, exhaleUp, exhaleRight, 0.03f, 0.15f);
                yield return new WaitForSeconds(0.06f);
            }

            yield return new WaitForSeconds(1.8f);
            _playing = false;
        }

        private static void SpawnSolidRing(Transform camT, Vector3 forward, Vector3 up, Vector3 right)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "WVC_VapeSmoke_SolidRing";
            StripPhysicsImmediate(ring);

            Vector3 mouthOrigin = camT.position + (forward * MouthForward) + (up * MouthDown) + (right * MouthRight);

            ring.transform.position = mouthOrigin;
            ring.transform.rotation = Quaternion.LookRotation(forward, up);

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

            float maxDistance = 1.2f;
            float finalSize = 0.55f;

            while (elapsed < lifetime)
            {
                if (ring == null) yield break;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / lifetime);

                float moveProgress = Mathf.Pow(progress, 0.45f);
                t.position = startPos + (travelDir * (moveProgress * maxDistance));

                float currentSize = Mathf.Lerp(0.03f, finalSize, Mathf.Pow(progress, 0.6f));
                t.localScale = Vector3.one * currentSize;

                if (Camera.main != null)
                {
                    t.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
                }

                if (mat != null)
                {
                    float alpha;
                    if (progress < 0.1f) alpha = Mathf.Lerp(0f, RingOpacity, progress / 0.1f);
                    else alpha = Mathf.Lerp(RingOpacity, 0f, (progress - 0.1f) / 0.9f);

                    Color c = new Color(0.98f, 0.99f, 1f, alpha);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                }

                yield return null;
            }

            if (ring != null) UnityEngine.Object.Destroy(ring);
        }

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

            int size = 256;
            _ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "WVC_SolidRing_Tex",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.48f;

            float ringCenter = 0.70f;
            float ringThickness = 0.18f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;

                    float diff = Mathf.Abs(dist - ringCenter);

                    float alpha = Mathf.Clamp01(1f - (diff / ringThickness));

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
