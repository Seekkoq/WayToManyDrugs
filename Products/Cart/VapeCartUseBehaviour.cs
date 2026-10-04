using System;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Products
{
    public sealed class VapeCartUseBehaviour : MonoBehaviour
    {
        private const float CooldownSeconds = 2.30f;

        private float _nextUseTime;

        public VapeCartUseBehaviour(IntPtr pointer)
            : base(pointer)
        {
        }

        private void Update()
        {
            if (!IsProbablyHeld())
                return;

            if (Time.time < _nextUseTime)
                return;

            if (CartAnimator.IsPlaying)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            Transform cartVisual =
                FindCartVisual();

            if (cartVisual == null)
            {


                return;
            }

            _nextUseTime =
                Time.time + CooldownSeconds;

            CartAnimator.PlayHit(cartVisual);

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Cart] Cart inhale triggered."
            );
        }

        private bool IsProbablyHeld()
        {
            if (gameObject == null ||
                !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (transform.position.y < -1000f)
                return false;

            Camera camera = Camera.main;

            if (camera == null)
                return false;

            return Vector3.Distance(
                transform.position,
                camera.transform.position
            ) < 3f;
        }

        private Transform FindCartVisual()
        {
            Transform[] transforms =
                GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform child = transforms[i];

                if (child == null)
                    continue;

                string childName =
                    child.name ?? string.Empty;

                if (childName.IndexOf(
                        "WVC_CartVisual_Equippable",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
