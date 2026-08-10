using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace CustomNPCExample.NPCs
{
    public static class LayerPathProbe
    {
        private static readonly string[] Bases =
        {
            "Avatar/Layers/Top/",
            "Avatar/Layers/Bottom/",
            "Avatar/Layers/Face/",
            "Avatar/Accessories/Head/",
            "Avatar/Accessories/Chest/",
            "Avatar/Accessories/Feet/",
            "Avatar/Accessories/Hands/",
            "Avatar/Accessories/Neck/",
            "Avatar/Accessories/Waist/",
            "Avatar/Accessories/Bottom/",
            "Avatar/Hair/",
        };

        private static readonly string[] Names =
        {
            // proven working
            "T-Shirt", "Jeans", "Face_Neutral",
            // guesses worth testing
            "Shirt", "LongSleeveShirt", "ButtonUp", "Polo", "TankTop",
            "Hoodie", "Sweater", "Vest", "Overalls", "Flannel",
            "Shorts", "Cargos", "Khakis", "Pants", "Slacks", "Sweatpants", "Skirt",
            "Cap", "Beanie", "Hat", "Cowboy", "Bandana", "Headphones",
            "Sunglasses", "Glasses", "Mask",
            "Boots", "Sneakers", "Sandals", "Shoes", "DressShoes",
            "Gloves", "Watch", "Rings",
            "Chain", "Necklace", "Scarf", "Tie",
            "Belt", "FannyPack",
            "Jacket", "CollarJacket", "Coat", "Blazer", "DenimJacket",
            "Buzzcut", "Spiky", "Long", "Bald", "Mohawk", "Afro",
            "Ponytail", "Bun", "Curly", "Bob", "Dreads",
            "FacialHair_Goatee", "Goatee", "Beard", "Mustache", "Stubble",
        };

        public static void Run()
        {
            var hits = new List<string>();
            int tested = 0;

            foreach (string b in Bases)
            {
                foreach (string n in Names)
                {
                    // flat form: Avatar/Layers/Top/T-Shirt
                    tested++;
                    if (Exists(b + n)) hits.Add(b + n);

                    // nested form: Avatar/Accessories/Feet/Sneakers/Sneakers
                    tested++;
                    string nested = b + n + "/" + n;
                    if (Exists(nested)) hits.Add(nested);
                }
            }

            MelonLogger.Msg($"[LayerProbe] Tested {tested} paths, found {hits.Count} valid.");
            foreach (string h in hits)
                MelonLogger.Msg("[LayerProbe] VALID -> " + h);

            try
            {
                string outFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "ValidLayerPaths.txt");

                File.WriteAllLines(outFile, hits);
                MelonLogger.Msg("[LayerProbe] Written to " + outFile);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[LayerProbe] Write failed: " + ex.Message);
            }
        }

        private static bool Exists(string path)
        {
            try
            {
                if (Resources.Load<GameObject>(path) != null) return true;
                if (Resources.Load<UnityEngine.Object>(path) != null) return true;
            }
            catch { }
            return false;
        }
    }
}