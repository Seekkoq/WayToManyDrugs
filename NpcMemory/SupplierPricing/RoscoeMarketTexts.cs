using System;
using System.Collections;
using System.Reflection;
using System.Text;
using CustomNPCExample.NPCs;
using CustomNPCExample.Products;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.SupplierPricing
{
    public static class RoscoeMarketTexts
    {

        private const string HeaderColor = "#253A73";
        private const string BodyColor = "#30333A";
        private const string UpColor = "#C62828";
        private const string DownColor = "#16834A";
        private const string FlatColor = "#B06B00";
        private const string TightColor = "#7A3E00";
        private const string SoftColor = "#27734B";
        private const string InfoColor = "#5A3D91";

        public static void SendMarketUpdate(PriceEntry[] entries, string reason)
        {
            if (entries == null || entries.Length == 0) return;

            string body = BuildMessage(entries, reason);

            global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Pricing] Roscoe market text prepared: " + body);
            MelonCoroutines.Start(SendRoutine(body));
        }

        private static string BuildMessage(
            PriceEntry[] entries,
            string reason)
        {
            float previousTotal = 0f;
            float currentTotal = 0f;

            StringBuilder lines = new StringBuilder();

            foreach (var entry in entries)
            {
                previousTotal += entry.PreviousPrice;
                currentTotal += entry.CurrentPrice;

                string name = GetShortName(entry.ItemId);

                bool up = entry.CurrentPrice > entry.PreviousPrice;
                bool down = entry.CurrentPrice < entry.PreviousPrice;

                string color =
                    up ? UpColor :
                    down ? DownColor :
                    FlatColor;

                string direction =
                    up ? "▲ UP" :
                    down ? "▼ DOWN" :
                    "• FLAT";

                lines.Append(
                    $"<color={color}>{name}  ${entry.CurrentPrice:0}  {direction}</color>\n"
                );
            }

            float delta = currentTotal - previousTotal;

            string marketLine;

            if (delta > 30f)
            {
                marketLine =
                    $"<color={TightColor}>▲ MARKET TIGHTENING</color>\n" +
                    $"<color={InfoColor}>Supply is getting thin. Buy early.</color>";
            }
            else if (delta < -30f)
            {
                marketLine =
                    $"<color={SoftColor}>▼ MARKET SOFTENING</color>\n" +
                    $"<color={InfoColor}>Good time to stock the drop.</color>";
            }
            else
            {
                marketLine =
                    $"<color={FlatColor}>• MARKET SHIFTED</color>\n" +
                    $"<color={InfoColor}>Same stock. Different rates.</color>";
            }

            return
                $"<color={HeaderColor}><b>ROSCOE — MARKET UPDATE</b></color>\n" +
                $"<color={BodyColor}>Today's supplier prices:</color>\n" +
                $"{lines}" +
                $"{marketLine}";
        }

        private static string GetShortName(string itemId)
        {
            if (string.Equals(itemId, MollyIngredients.SafroleId, StringComparison.OrdinalIgnoreCase)) return "Safrole";
            if (string.Equals(itemId, MollyIngredients.PmkId, StringComparison.OrdinalIgnoreCase)) return "PMK";
            if (string.Equals(itemId, MollyIngredients.PmkRefinedId, StringComparison.OrdinalIgnoreCase)) return "Refined PMK";
            if (string.Equals(itemId, MollyIngredients.PmkLabGradeId, StringComparison.OrdinalIgnoreCase)) return "Lab PMK";
            return "Stock";
        }

        private static IEnumerator SendRoutine(string body)
        {
            yield return new WaitForSeconds(1.5f);

            for (int attempt = 0; attempt < 25; attempt++)
            {
                if (TrySendToPhone(body))
                    yield break;

                yield return new WaitForSeconds(0.3f);
            }

            HintDisplay.Instance?.ShowHint_20s(body);
        }

        private static bool TrySendToPhone(string body)
        {
            try
            {
                var roscoe = RoscoeBellweather.Instance;
                if (roscoe == null || roscoe.gameObject == null) return false;

                var npc = roscoe.gameObject.GetComponent<NPC>()
                       ?? roscoe.gameObject.GetComponentInChildren<NPC>(true);
                if (npc == null) return false;

                var conversation = npc.MSGConversation;
                if (conversation == null) return false;

                Message msg = new Message(body, Message.ESenderType.Other);

                conversation.SendMessage(msg, true, true);

                global::CustomNPCExample.Utils.WvcLog.Msg("[WVC Pricing] Sent market update via Roscoe's phone conversation.");
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
