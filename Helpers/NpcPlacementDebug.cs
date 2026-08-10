using System.Globalization;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public static class NpcPlacementDebug
    {
        public static void PrintPlacementFromCamera()
        {
            Camera cam = Camera.main;

            if (cam == null)
            {
                MelonLogger.Warning(
                    "[NPC Placement] Camera.main is null."
                );
                return;
            }

            Vector3 cameraPos =
                cam.transform.position;

            // Approximate floor / feet position from camera height
            Vector3 standPos = cameraPos;
            standPos.y -= 1.6f;

            float yaw =
                cam.transform.eulerAngles.y;

            MelonLogger.Msg(
                "[NPC Placement] CameraPos = " +
                FormatVector(cameraPos)
            );

            MelonLogger.Msg(
                "[NPC Placement] StandPos = " +
                FormatVector(standPos)
            );

            MelonLogger.Msg(
                "[NPC Placement] Yaw = " +
                FormatFloat(yaw)
            );

            MelonLogger.Msg(
                "[NPC Placement] COPY THIS:"
            );

            MelonLogger.Msg(
                ".WithSpawnPosition(" +
                "new Vector3(" +
                FormatFloat(standPos.x) + "f, " +
                FormatFloat(standPos.y) + "f, " +
                FormatFloat(standPos.z) + "f), " +
                "Quaternion.Euler(0f, " +
                FormatFloat(yaw) +
                "f, 0f))"
            );
        }

        private static string FormatVector(Vector3 v)
        {
            return "(" +
                   FormatFloat(v.x) + ", " +
                   FormatFloat(v.y) + ", " +
                   FormatFloat(v.z) + ")";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture
            );
        }
    }
}