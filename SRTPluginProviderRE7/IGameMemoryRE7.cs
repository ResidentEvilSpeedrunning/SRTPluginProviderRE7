using System;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;
using SRTPluginProviderRE7.Structs.GameStructs;

namespace SRTPluginProviderRE7
{
    public interface IGameMemoryRE7
    {
        string GameName { get; }
        string VersionInfo { get; }
        GamePlayer Player { get; set; }
        float RankScore { get; set; }
        int Rank { get; }
        int PlayerInventoryCount { get; set; }
        int PlayerInventorySlots { get; set; }
        int PlayerCurrentSelectedInventorySlots { get; set; }
        string RoomID { get; set; }
        EnemyHP[] EnemyHealth { get; set; }
        JackEyeHP[] JackHP { get; set; }
        InventoryEntry[] PlayerInventory { get; set; }

    }
}
