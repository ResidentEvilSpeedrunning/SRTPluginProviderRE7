using System;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;

namespace SRTPluginProviderRE7
{
    public interface IGameMemoryRE7
    {
        string VersionInfo { get; }
        string MapName { get; set; }
        float PlayerCurrentHealth { get; set; }
        float PlayerMaxHealth { get; set; }
        float RankScore { get; set; }
        int Rank { get; }
        int EnemyCount { get; set; }
        int PlayerInventoryCount { get; set; }
        int PlayerInventorySlots { get; set; }
        int PlayerCurrentSelectedInventorySlots { get; set; }        
        EnemyHP[] EnemyHealth { get; set; }
        InventoryEntry[] PlayerInventory { get; set; }
    }
}
