using System;

namespace CustomNPCExample.NpcMemory
{
    [Serializable]
    public sealed class NpcMemoryData
    {
        public int Strikes { get; set; }

        public long LastStrikeUtcTicks { get; set; }

        public bool RequiresSample { get; set; }
    }
}