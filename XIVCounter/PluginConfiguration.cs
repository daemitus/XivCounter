using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace XIVCounter
{
    public class PluginConfiguration : IPluginConfiguration
    {
        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public int Version { get; set; } = 1;

        public int UpdateWaitMs { get; set; } = 1000;

        public int BigUpdateCounter { get; set; } = 3;

        public int AutoSaveCounter { get; set; } = 30;

        public int MaxTargetCache { get; set; } = 5;

        public int ShiftMod { get; set; } = 10;

        public int CtrltMod { get; set; } = 100;

        public int BothMod { get; set; } = 1000;

        internal List<Counter> Counters { get; set; } = new List<Counter>();

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;

        public void SaveSettings() => this.pluginInterface.SavePluginConfig(this);
    }
}
