using System;
using MelonLoader;
using S1API.Law;
using UnityEngine;

using ApiPlayer = S1API.Entities.Player;

namespace CustomNPCExample.Law
{
    public static class UndercoverBust
    {
        private static float _lastBustTime = -999f;

        public static bool UseDirectWantedLevel = false;

        public static void Trigger(string sourceName = null)
        {
            if (Time.realtimeSinceStartup - _lastBustTime < 2f)
                return;

            _lastBustTime = Time.realtimeSinceStartup;

            ApiPlayer player = null;

            try
            {
                player = ApiPlayer.Local;
            }
            catch { }

            if (player == null)
            {


                return;
            }

            bool called = false;

            try
            {
                LawManager.CallPolice(player);
                called = true;
            }
            catch (Exception)
            {

            }

            if (UseDirectWantedLevel)
            {
                try
                {
                    LawManager.SetWantedLevel(
                        player,
                        PursuitLevel.Arresting
                    );
                }
                catch (Exception)
                {

                }
            }

            global::CustomNPCExample.Utils.WvcLog.Msg(
                "[WVC Undercover] Bust triggered" +
                (
                    string.IsNullOrEmpty(sourceName)
                        ? "."
                        : " by " + sourceName + "."
                ) +
                " CallPolice=" +
                called +
                " DirectWanted=" +
                UseDirectWantedLevel
            );
        }
    }
}
