using System;
using System.Globalization;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;

namespace SRTPluginProviderRE7
{
    public class GameMemoryRE7 : IGameMemoryRE7
    {
        public string MapName { get; set; }
        public float CurrentDA { get; set; }
        public float CurrentHP { get; set; }
        public float MaxHP { get; set; }
        public int MrEverything { get; set; }
        public int FileCount { get; set; }
        public int CoinCount { get; set; }
        public int EnemyCount { get; set; }
        public EnemyHP[] EnemyHealth { get; set; }
        public JackEyeHP[] JackEyeHealth { get; set; }
        public int PlayerInventoryCount { get; set; }
        public int PlayerInventorySlots { get; set; }
        public int PlayerCurrentSelectedInventorySlots { get; set; }
        public InventoryEntry[] PlayerInventory { get; set; }
        public long Timestamp { get; set; }
        public int GameState { get; set; }
        public int GameplayState { get; set; }
        public int GameInit { get; set; }

        public GameMemoryRE7()
        {
            CurrentDA = 0;
            MapName = "";
            CurrentHP = 0;
            MaxHP = 0;
            MrEverything = 0;
            FileCount = 0;
            CoinCount = 0;
            PlayerInventorySlots = 12;
            PlayerInventoryCount = 0;
            PlayerCurrentSelectedInventorySlots = 0;
            Timestamp = 0;
            GameState = 0;
        }
    }
}
