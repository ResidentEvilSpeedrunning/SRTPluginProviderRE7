using System;
using System.Globalization;
using System.Collections.Generic;
using SRTPluginProviderRE7.Structs;
using System.Diagnostics;
using System.Reflection;
using SRTPluginProviderRE7.Structs.GameStructs;

namespace SRTPluginProviderRE7
{
    public class GameMemoryRE7 : IGameMemoryRE7
    {
        public string GameName => "RE7";
        public string VersionInfo => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        public GamePlayer Player { get => _player; set => _player = value; }
        internal GamePlayer _player;
        public float RankScore { get => _rankScore; set => _rankScore = value; }
        internal float _rankScore;
        public int Rank => (int)Math.Floor(_rankScore / 1000);
        public int PlayerInventoryCount { get => _playerInventoryCount; set => _playerInventoryCount = value; }
        internal int _playerInventoryCount;
        public int PlayerInventorySlots { get => _playerInventorySlots; set => _playerInventorySlots = value; }
        internal int _playerInventorySlots;
        public int PlayerCurrentSelectedInventorySlots { get => _playerCurrentSelectedInventorySlots; set => _playerCurrentSelectedInventorySlots = value; }
        internal int _playerCurrentSelectedInventorySlots;       
        public string RoomID { get => _roomID; set => _roomID = value; }
        internal string _roomID;
        public EnemyHP[] EnemyHealth { get => _enemyHealth; set => _enemyHealth = value; }
        internal EnemyHP[] _enemyHealth;
        public JackEyeHP[] JackHP { get => _jackHP; set => _jackHP = value; }
        internal JackEyeHP[] _jackHP;
        public InventoryEntry[] PlayerInventory { get; set; }
    }
}
