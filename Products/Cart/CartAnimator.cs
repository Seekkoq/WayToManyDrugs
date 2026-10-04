using System.Collections;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public static class CartAnimator
    {
        private static bool _playing;
        public static bool IsPlaying => _playing;

        private static readonly Vector3 InhaleOffset = new Vector3(-0.070f, 0.05f, -0.08f);
        private static readonly Vector3 InhaleRotation = new Vector3(-73f, 6f, 0f);

        public static void PlayHit(Transform cartTransform)
        {
            if (_playing) return;
            if (cartTransform == null) return;

            MelonCoroutines.Start(AnimateCart(cartTransform));
        }

        private static IEnumerator AnimateCart(Transform cart)
        {
            _playing = true;

            Vector3 startPos = cart.localPosition;
            Quaternion startRot = cart.localRotation;

            Vector3 targetPos = startPos + InhaleOffset;
            Quaternion targetRot = startRot * Quaternion.Euler(InhaleRotation);

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] Inhale animation started.");

            yield return MoveTo(cart, startPos, targetPos, startRot, targetRot, 0.45f);

            yield return new WaitForSeconds(1.2f);

            CartVapor.PlayVapor();
            VapeCartEffectManager.TriggerEffect();

            yield return MoveTo(cart, targetPos, startPos, targetRot, startRot, 0.37f);

            if (cart != null)
            {
                cart.localPosition = startPos;
                cart.localRotation = startRot;
            }

            _playing = false;
            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Cart] Inhale animation complete.");
        }

        private static IEnumerator MoveTo(
            Transform t, Vector3 fromPos, Vector3 toPos,
            Quaternion fromRot, Quaternion toRot, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (t == null) yield break;

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                progress = progress * progress * (3f - 2f * progress);

                t.localPosition = Vector3.Lerp(fromPos, toPos, progress);
                t.localRotation = Quaternion.Slerp(fromRot, toRot, progress);

                yield return null;
            }
        }
    }
}
