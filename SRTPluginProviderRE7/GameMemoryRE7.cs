using System;
using System.Globalization;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;
using System.Diagnostics;
using System.Reflection;

namespace SRTPluginProviderRE7
{
    public class GameMemoryRE7 : IGameMemoryRE7
    {
        public string GameName => "RE7";
        public string VersionInfo => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        public string MapName { get => _mapName; set => _mapName = value; }
        internal string _mapName;
        public float PlayerCurrentHealth { get => _playerCurrentHealth; set => _playerCurrentHealth = value; }
        internal float _playerCurrentHealth;
        public float PlayerMaxHealth { get => _playerMaxHealth; set => _playerMaxHealth = value; }
        internal float _playerMaxHealth;
        public float RankScore { get => _rankScore; set => _rankScore = value; }
        internal float _rankScore;
        public int Rank => (int)Math.Floor(_rankScore / 1000);
        public int EnemyCount { get => _enemyCount; set => _enemyCount = value; }
        internal int _enemyCount;
        public int PlayerInventoryCount { get => _playerInventoryCount; set => _playerInventoryCount = value; }
        internal int _playerInventoryCount;
        public int PlayerInventorySlots { get => _playerInventorySlots; set => _playerInventorySlots = value; }
        internal int _playerInventorySlots;
        public int PlayerCurrentSelectedInventorySlots { get => _playerCurrentSelectedInventorySlots; set => _playerCurrentSelectedInventorySlots = value; }
        internal int _playerCurrentSelectedInventorySlots;       
        public EnemyHP[] EnemyHealth { get; set; }
        public InventoryEntry[] PlayerInventory { get; set; }
    }
}
