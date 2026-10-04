using UnityEngine;

using NativeDraggableConstraint = Il2CppScheduleOne.PlayerTasks.DraggableConstraint;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Cleans a cloned template of the parts that only ever make sense on a live object.
    ///
    /// The game's station-item templates carry a DraggableConstraint. The game uses it to keep the
    /// item aligned to the container it is being dragged onto. A clone of that template is dragged by
    /// nobody and belongs to no container, so the constraint sits there with nothing to align to and
    /// throws a null reference out of LateUpdate on every frame. Seven station items did that at once
    /// and produced 57,015 exceptions in a single session, which is what the frame rate was going on.
    ///
    /// The constraint is removed from the clone rather than switched off, so nothing is left ticking
    /// that was only ever meant to run on a live object. The sweep in
    /// <see cref="CustomNPCExample.Qol.WvcExceptionGuards"/> stays as the catch-all for the templates
    /// that are not cloned through the helpers that call this.
    /// </summary>
    internal static class WvcCloneSanitizer
    {
        /// <summary>Removes every drag constraint the clone brought with it, at any depth.</summary>
        public static void Strip(GameObject clone)
        {
            if (clone == null)
                return;

            try
            {
                NativeDraggableConstraint[] constraints =
                    clone.GetComponentsInChildren<NativeDraggableConstraint>(true);

                if (constraints == null)
                    return;

                for (int i = 0; i < constraints.Length; i++)
                {
                    if (constraints[i] != null)
                        UnityEngine.Object.Destroy(constraints[i]);
                }
            }
            catch { }
        }
    }
}
