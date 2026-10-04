using MelonLoader;
using S1API.Map;
using S1API.Map.Buildings;

namespace CustomNPCExample.NPCs
{
    internal static class NPCBuildingLookup
    {
        private static System.Collections.Generic.List<Building> _cachedBuildings;

        public static Building Get(string buildingName)
        {
            if (string.IsNullOrWhiteSpace(buildingName))
                return Building.Get<HylandBank>();

            Building building = Building.GetByName(buildingName);
            if (building != null)
                return building;

            try
            {
                if (_cachedBuildings == null || _cachedBuildings.Count == 0)
                {
                    _cachedBuildings = new System.Collections.Generic.List<Building>();
                    var all = Building.GetAll();
                    if (all != null)
                    {
                        foreach (var b in all)
                        {
                            if (b != null) _cachedBuildings.Add(b);
                        }
                    }
                }

                string targetNorm = Normalize(buildingName);

                foreach (var b in _cachedBuildings)
                {
                    if (b != null && Normalize(b.Name) == targetNorm)
                        return b;
                }

                foreach (var b in _cachedBuildings)
                {
                    if (b != null)
                    {
                        string bNorm = Normalize(b.Name);
                        if (bNorm.Contains(targetNorm) || targetNorm.Contains(bNorm))
                            return b;
                    }
                }
            }
            catch (System.Exception)
            {

            }

            string lower = buildingName.ToLowerInvariant();
            if (lower.Contains("bait") || lower.Contains("tackle"))
                return Building.Get<RandysBaitTackle>() ?? Building.Get<WestGasMart>();
            if (lower.Contains("gas"))
                return Building.Get<WestGasMart>();
            if (lower.Contains("pawn"))
                return Building.Get<PawnShop>();
            if (lower.Contains("bar"))
                return Building.Get<BudsBar>();
            if (lower.Contains("motel"))
                return Building.Get<MotelOffice>();
            if (lower.Contains("chinese") || lower.Contains("restaurant"))
                return Building.Get<ChineseRestaurant>();
            if (lower.Contains("arcade"))
                return Building.Get<Arcade>();
            if (lower.Contains("nightclub") || lower.Contains("club"))
                return Building.Get<Nightclub>();
            if (lower.Contains("supermarket") || lower.Contains("grocery"))
                return Building.Get<Supermarket>();



            return Building.Get<WestGasMart>() ?? Building.Get<HylandBank>();
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.ToLowerInvariant()
                .Replace("&", "and")
                .Replace("'", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("#", "")
                .Replace(".", "");
        }
    }
}
