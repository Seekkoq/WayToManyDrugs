using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    /// <summary>
    /// Rolls a product's icon model so the face that carries its artwork looks at the native icon
    /// camera.
    ///
    /// S1API renders generated item icons from the loose pose, and the loose pose has to work for
    /// an item lying on a table: the Xanax bar ends up flat with the moulded "XANAX" facing the
    /// sky, which is edge-on to the icon rig's camera, so the icon came out as a blank capsule.
    /// The rig only exists once a scene is running, so the rotation is computed here, inside the
    /// icon capture prefix, from the live camera and container of the native IconGenerator.
    ///
    /// The wrapper's axes are fixed by ProductBuilder: +X is the long axis of the bar (the fit
    /// node yaws the GLB onto it) and +Y is the artwork face. Pointing +Y at the camera therefore
    /// turns the imprint towards the lens and the long axis along the screen's horizontal.
    /// </summary>
    public static class WvcIconPose
    {
        /// <summary>
        /// How far the artwork face is rolled away from a dead-on view. Zero stares straight down
        /// on the face (maximum legibility, no visible depth), larger values show more of the
        /// model's side.
        /// </summary>
        private const float TiltDegrees = 30f;

        /// <summary>
        /// Flips which end of the model points to the right of the icon, which also turns the
        /// artwork 180 degrees on screen. Set to true when an imprint comes out upside down.
        /// </summary>
        private const bool FlipLongAxis = false;

        /// <summary>Node ProductBuilder hangs under every product wrapper to hold the unit fit.</summary>
        private const string FitSuffix = "_Fit";

        private static readonly HashSet<string> Watched =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static bool _loggedNoRig;
        private static bool _loggedApplied;

        /// <summary>
        /// Marks a product wrapper (and the clones made from it) as one whose icon should be
        /// turned to present its artwork face. Called by ProductBuilder for models that opt in.
        /// </summary>
        public static void Watch(string assetName)
        {
            if (!string.IsNullOrEmpty(assetName))
                Watched.Add(assetName);
        }

        /// <summary>
        /// Turns a model that is about to be rendered by the icon rig. Runs as part of the icon
        /// capture prefix, so it only ever touches a clone that is destroyed right after the
        /// capture - never the loose, stored or held visuals.
        /// </summary>
        public static void Apply(Transform model, Il2CppScheduleOne.DevUtilities.IconGenerator generator)
        {
            if (model == null || !IsWatched(model))
                return;

            try
            {
                if (generator == null)
                {
                    generator = UnityEngine.Object.FindObjectOfType<
                        Il2CppScheduleOne.DevUtilities.IconGenerator>();
                }

                Transform container = null;
                Camera camera = null;

                if (generator != null)
                {
                    try { container = generator.ItemContainer; } catch { }
                    try { camera = generator.CameraPosition; } catch { }
                }

                if (container == null || camera == null)
                {
                    if (!_loggedNoRig)
                    {
                        _loggedNoRig = true;

                    }

                    return;
                }

                Vector3 toCamera = camera.transform.position - container.position;

                if (toCamera.sqrMagnitude < 0.0001f)
                    return;

                // IconFactory restores the model's local rotation after parenting it to the item
                // container, so the axes the renderer ends up using are container-local ones.
                Vector3 toCameraLocal = container.InverseTransformDirection(toCamera.normalized);
                Vector3 cameraUpLocal = container.InverseTransformDirection(camera.transform.up);

                if (cameraUpLocal.sqrMagnitude < 0.0001f)
                    cameraUpLocal = Vector3.up;

                cameraUpLocal.Normalize();

                // In Unity's basis the camera's right axis is cross(up, forward), so with the rig
                // looking from the camera towards the container this is the axis the icon shows to
                // the right of the frame.
                Vector3 longAxis = Vector3.Cross(toCameraLocal, cameraUpLocal);

                if (longAxis.sqrMagnitude < 0.0001f)
                    longAxis = Vector3.Cross(toCameraLocal, Vector3.up);

                if (longAxis.sqrMagnitude < 0.0001f)
                    longAxis = Vector3.right;

                longAxis.Normalize();

                if (FlipLongAxis)
                    longAxis = -longAxis;

                Vector3 screenUp = Vector3.Cross(longAxis, toCameraLocal).normalized;

                float tilt = TiltDegrees * Mathf.Deg2Rad;

                // Turning the artwork normal from the view axis towards the camera's up axis keeps
                // the face nearly square to the lens while leaving the near side in view, the way
                // a product photo is staged.
                Vector3 faceNormal =
                    (toCameraLocal * Mathf.Cos(tilt) + screenUp * Mathf.Sin(tilt)).normalized;

                Vector3 forward = Vector3.Cross(longAxis, faceNormal).normalized;

                model.localRotation = Quaternion.LookRotation(forward, faceNormal);

                if (!_loggedApplied)
                {
                    _loggedApplied = true;

                }
            }
            catch (Exception)
            {
                if (!_loggedNoRig)
                {
                    _loggedNoRig = true;

                }
            }
        }

        /// <summary>
        /// True when the transform belongs to an opted-in product wrapper. The wrapper itself is
        /// renamed by some pipelines (the presentation workbench calls its clone "Icon"), so the
        /// fit node ProductBuilder hangs under it is the reliable marker.
        /// </summary>
        private static bool IsWatched(Transform model)
        {
            if (Watched.Count == 0)
                return false;

            try
            {
                int children = model.childCount;

                for (int i = 0; i < children; i++)
                {
                    Transform child = model.GetChild(i);

                    if (child == null)
                        continue;

                    string childName = child.name;

                    if (string.IsNullOrEmpty(childName) ||
                        !childName.EndsWith(FitSuffix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string owner = childName
                        .Substring(0, childName.Length - FitSuffix.Length)
                        .Trim();

                    if (Watched.Contains(owner))
                        return true;
                }

                string name = model.name;

                if (string.IsNullOrEmpty(name))
                    return false;

                // Object.Instantiate suffixes clones with "(Clone)".
                int cloneSuffix = name.IndexOf('(');

                if (cloneSuffix > 0)
                    name = name.Substring(0, cloneSuffix);

                return Watched.Contains(name.Trim());
            }
            catch
            {
                return false;
            }
        }
    }
}
