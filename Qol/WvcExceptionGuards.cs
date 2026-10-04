using System;
using MelonLoader;
using UnityEngine;

using NativeDraggableConstraint = Il2CppScheduleOne.PlayerTasks.DraggableConstraint;

namespace CustomNPCExample.Qol
{
    /// <summary>
    /// Stops the game's own per-frame exceptions from eating the frame rate.
    ///
    /// The game's drag constraint aligns whatever is being dragged to the container under it, from
    /// LateUpdate, once a frame. When its Container is null - which is what an object left in the
    /// world by a product registration ends up with - that alignment dereferences null and throws.
    /// One session had 57,015 of them: 4.6 MB of log from one run, and an exception and a stack trace
    /// captured on every single frame. That is what the low frame rate actually was.
    ///
    /// A Harmony finalizer on the interop method was tried first and does not work. Unity calls
    /// LateUpdate from native code, and the managed stub the finalizer was attached to is not on that
    /// path: the log said the patch was installed and the exceptions carried on regardless.
    ///
    /// So the component itself is dealt with instead. A constraint with no container can never align
    /// to anything - it is already broken - so its alignment is switched off. That stops the call that
    /// throws while leaving the rest of the constraint alone: nothing is destroyed and no method is
    /// patched. What was fixed is written to the log together with the object it sat on, so whatever
    /// left it there can be found and fixed properly.
    /// </summary>
    internal static class WvcExceptionGuards
    {
        /// <summary>How often to look for broken constraints, in seconds.</summary>
        private const float ScanInterval = 3f;

        /// <summary>How many of them are named in the log before it goes quiet.</summary>
        private const int LogFirst = 6;

        private static float _timer;
        private static int _fixed;
        private static int _scans;

        public static void ApplyPatch()
        {

        }

        /// <summary>Runs from the mod's update. Cheap: one throttled scan of a handful of components.</summary>
        public static void Update()
        {
            _timer -= Time.unscaledDeltaTime;

            if (_timer > 0f)
                return;

            // Look often while the world is settling - this appears during start-up - and rarely
            // afterwards, when it is only worth a glance in case another one turns up.
            _timer = _scans < 20 ? ScanInterval : 15f;
            _scans++;

            try
            {
                NeutraliseBroken();
            }
            catch (Exception)
            {

            }
        }

        private static void NeutraliseBroken()
        {
            NativeDraggableConstraint[] all;

            try
            {
                all = UnityEngine.Object.FindObjectsOfType<NativeDraggableConstraint>(true);
            }
            catch
            {
                return;
            }

            if (all == null)
                return;

            for (int i = 0; i < all.Length; i++)
            {
                NativeDraggableConstraint constraint = all[i];

                if (constraint == null)
                    continue;

                bool hasContainer;
                bool aligning;

                try
                {
                    hasContainer = constraint.Container != null;
                    aligning = constraint.AlignUpToContainerPlane;
                }
                catch
                {
                    continue;
                }

                if (hasContainer || !aligning)
                    continue;

                try
                {
                    constraint.AlignUpToContainerPlane = false;

                    _fixed++;

                    if (_fixed <= LogFirst)
                    {

                    }
                    else if (_fixed == LogFirst + 1)
                    {

                    }
                }
                catch { }
            }
        }

        /// <summary>The object the constraint sits on, with a few of its parents, for the log.</summary>
        private static string Describe(NativeDraggableConstraint constraint)
        {
            try
            {
                Transform t = constraint.transform;

                if (t == null)
                    return "<no transform>";

                string path = t.name;

                for (int up = 0; up < 8 && t.parent != null; up++)
                {
                    t = t.parent;
                    path = t.name + "/" + path;
                }

                return path;
            }
            catch
            {
                return "<unnamed>";
            }
        }
    }
}
