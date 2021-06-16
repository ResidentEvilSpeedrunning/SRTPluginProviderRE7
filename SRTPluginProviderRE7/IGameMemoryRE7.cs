using System;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;

namespace SRTPluginProviderRE7
{
    public interface IGameMemoryRE7
    {
        string MapName { get; set; }
        float CurrentDA { get; set; }
        float CurrentHP { get; set; }
        float MaxHP { get; set; }
        int MrEverything { get; set; }
        int FileCount { get; set; }
        int CoinCount { get; set; }
        int EnemyCount { get; set; }
        EnemyHP[] EnemyHealth { get; set; }
        JackEyeHP[] JackEyeHealth { get; set; }
        int PlayerInventoryCount { get; set; }
        int PlayerInventorySlots { get; set; }
        int PlayerCurrentSelectedInventorySlots { get; set; }
        InventoryEntry[] PlayerInventory { get; set; }
        long Timestamp { get; set; }
        int GameState { get; set; }
        int GameplayState { get; set; }
        int GameInit { get; set; }
    }
}
