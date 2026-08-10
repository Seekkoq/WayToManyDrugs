using MelonLoader;
using S1API.Map;
using S1API.Map.Buildings;

namespace CustomNPCExample.NPCs
{
    internal static class NPCBuildingLookup
    {
        public static Building Get(string exactBuildingName)
        {
            Building building = Building.GetByName(exactBuildingName);

            if (building != null)
                return building;

            MelonLogger.Warning(
                $"[Westville Connection] Building '{exactBuildingName}' not found. Falling back to Hyland Bank."
            );

            return Building.Get<HylandBank>();
        }
    }
}