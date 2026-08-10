using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.Utils
{
    public static class InventoryDump
    {
        public static void Dump()
        {
            MelonLogger.Msg("===== INVENTORY DUMP START =====");

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player == null)
            {
                MelonLogger.Msg("[Dump] No Player GameObject found by tag or name.");
                DumpS1ApiTypes();
                MelonLogger.Msg("===== INVENTORY DUMP END =====");
                return;
            }

            MelonLogger.Msg("[Dump] Player object: " + player.name);

            var behaviours =
                player.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var mb in behaviours)
            {
                if (mb == null) continue;

                Type t = mb.GetType();

                // Only log components that plausibly relate to items
                string n = t.Name.ToLowerInvariant();
                bool relevant =
                    n.Contains("inventory") ||
                    n.Contains("item") ||
                    n.Contains("hotbar") ||
                    n.Contains("slot") ||
                    n.Contains("player");

                if (!relevant) continue;

                MelonLogger.Msg("[Dump] Component: " + t.FullName);

                var methods = t.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                foreach (var m in methods)
                {
                    string mn = m.Name.ToLowerInvariant();
                    if (!mn.Contains("add") &&
                        !mn.Contains("give") &&
                        !mn.Contains("grant") &&
                        !mn.Contains("insert") &&
                        !mn.Contains("spawn"))
                        continue;

                    var ps = m.GetParameters();
                    string sig = "";
                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (i > 0) sig += ", ";
                        sig += ps[i].ParameterType.Name + " " + ps[i].Name;
                    }

                    MelonLogger.Msg(
                        "    -> " + m.Name + "(" + sig + ")"
                    );
                }
            }

            DumpS1ApiTypes();
            MelonLogger.Msg("===== INVENTORY DUMP END =====");
        }

        private static void DumpS1ApiTypes()
        {
            MelonLogger.Msg("[Dump] Scanning S1API for item/inventory types...");

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string an = asm.GetName().Name;
                if (an == null) continue;
                if (!an.StartsWith("S1API")) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    string tn = t.Name.ToLowerInvariant();
                    if (!tn.Contains("inventory") &&
                        !tn.Contains("item") &&
                        !tn.Contains("product"))
                        continue;

                    // Log the type and any give/add-style public methods
                    var methods = t.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.Static |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly
                    );

                    bool loggedHeader = false;

                    foreach (var m in methods)
                    {
                        string mn = m.Name.ToLowerInvariant();
                        if (!mn.Contains("add") &&
                            !mn.Contains("give") &&
                            !mn.Contains("grant"))
                            continue;

                        if (!loggedHeader)
                        {
                            MelonLogger.Msg("[Dump] S1API type: " + t.FullName);
                            loggedHeader = true;
                        }

                        var ps = m.GetParameters();
                        string sig = "";
                        for (int i = 0; i < ps.Length; i++)
                        {
                            if (i > 0) sig += ", ";
                            sig += ps[i].ParameterType.Name + " " + ps[i].Name;
                        }

                        MelonLogger.Msg(
                            "    -> " +
                            (m.IsStatic ? "static " : "") +
                            m.Name + "(" + sig + ")"
                        );
                    }
                }
            }
        }
    }
}