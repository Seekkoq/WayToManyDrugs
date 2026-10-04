using System;
using UnityEngine;

using NativeMovement = Il2CppScheduleOne.PlayerScripts.PlayerMovement;
using NativePlayer = Il2CppScheduleOne.PlayerScripts.Player;
using NativeStack = Il2CppScheduleOne.Tools.FloatStack;

using WvcLog = CustomNPCExample.Utils.WvcLog;

namespace CustomNPCExample.Products
{
    /// <summary>
    /// Heavy-limbed walking for the Xanax sedation stages, pushed into the game's own
    /// <c>PlayerMovement.MoveSpeedMultiplierStack</c> so the penalty is removable by label and
    /// stacks cleanly with anything else that touches movement.
    ///
    /// Two deliberate choices here:
    /// - Everything is typed interop, never reflection. The first version resolved the stack and
    ///   its nested entry type by reflection, which walked the whole assembly's type list on every
    ///   dose change and produced visible frame spikes.
    /// - It lives outside <see cref="XanaxScreenEffect"/> because Il2CppInterop has to inject the
    ///   MonoBehaviour the mod adds, and reflection/System.Type members on that class break the
    ///   injection.
    /// </summary>
    internal static class XanaxMovementImpairment
    {
        private const string Label = "WVC_Xanax_Sedation";
        private const int EntryOrder = 0;

        private static NativeMovement _movement;
        private static bool _disabled;
        private static bool _waitingLogged;
        private static bool _applied;
        private static float _value = 1f;

        internal static void Apply(float value)
        {
            float target = Mathf.Clamp(value, 0.2f, 1f);

            if (_applied && Mathf.Abs(target - _value) < 0.01f)
                return;

            NativeStack stack = GetStack();

            if (stack == null)
                return;

            try
            {
                stack.Remove(Label);
                stack.Add(
                    new NativeStack.StackEntry(
                        Label,
                        target,
                        NativeStack.EStackMode.Multiplicative,
                        EntryOrder
                    )
                );

                _applied = true;
                _value = target;
            }
            catch (Exception ex)
            {
                Disable(ex.Message);
            }
        }

        internal static void Clear()
        {
            if (!_applied)
                return;

            _applied = false;
            _value = 1f;

            NativeStack stack = GetStack();

            if (stack == null)
                return;

            try
            {
                stack.Remove(Label);
            }
            catch (Exception ex)
            {
                Disable(ex.Message);
            }
        }

        private static NativeStack GetStack()
        {
            if (_disabled)
                return null;

            if (_movement == null && !ResolveMovement())
                return null;

            try
            {
                NativeStack stack = _movement.MoveSpeedMultiplierStack;

                if (stack == null)
                {
                    Disable("the game returned no movement multiplier stack");
                    return null;
                }

                return stack;
            }
            catch (Exception ex)
            {
                Disable(ex.Message);
                return null;
            }
        }

        private static bool ResolveMovement()
        {
            try
            {
                NativePlayer local = NativePlayer.Local;

                if (local == null)
                    return false;

                NativeMovement fallback = null;

                foreach (NativeMovement candidate in Resources.FindObjectsOfTypeAll<NativeMovement>())
                {
                    if (candidate == null)
                        continue;

                    if (fallback == null)
                        fallback = candidate;

                    if (candidate.Player == local)
                    {
                        _movement = candidate;
                        return true;
                    }
                }

                if (fallback != null)
                {
                    _movement = fallback;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Disable(ex.Message);
                return false;
            }

            if (!_waitingLogged)
            {
                _waitingLogged = true;


            }

            return false;
        }

        private static void Disable(string reason)
        {
            if (_disabled)
                return;

            _disabled = true;


        }
    }
}
