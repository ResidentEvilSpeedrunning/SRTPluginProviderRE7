using ProcessMemory;
using System;
using System.Diagnostics;
using SRTPluginProviderRE7.Structs;
using SRTPluginProviderRE7.Structs.GameStructs;
using System.Linq;

namespace SRTPluginProviderRE7
{
    internal class GameMemoryRE7Scanner : IDisposable
    {
        private static readonly int MAX_ENTITIES = 64;
        private static readonly int MAX_ITEMS = 24;

        // Variables
        private ProcessMemoryHandler memoryAccess;
        private GameMemoryRE7 gameMemoryValues;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;

        // Pointer Address Variables
        private long difficultyAdjustment;
        private long hitPoints;
        //private long enemyHitPoints;
        //private long selectedSlot;
        //private long itemCount;
        //private long mapName;
        //private long bagCount;

        // Pointer Classes
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerDA { get; set; }
        //private MultilevelPointer PointerMapName { get; set; }
        private MultilevelPointer PointerHP { get; set; }
        //private MultilevelPointer PointerBagCount { get; set; }
        //private MultilevelPointer PointerInventoryCount { get; set; }
        //private MultilevelPointer PointerInventorySlotSelected { get; set; }
        //private MultilevelPointer PointerEnemyEntryCount { get; set; }
        //private MultilevelPointer[] PointerEnemyEntries { get; set; }
        //private MultilevelPointer[] PointerItemNames { get; set; }
        //private MultilevelPointer[] PointerItemInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        internal GameMemoryRE7Scanner(int? pid = null)
        {
            gameMemoryValues = new GameMemoryRE7();

            if (pid != null)
            {
                Initialize(pid.Value);
            }

            // Setup the pointers.
            
        }

        internal void Initialize(int pid)
        {
            SelectPointerAddresses(GameHashes.DetectVersion(Process.GetProcessesByName("re7").FirstOrDefault().MainModule.FileName));
            memoryAccess = new ProcessMemoryHandler(pid, false);

            if (ProcessRunning)
            {
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.
                PointerDA = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + difficultyAdjustment));
                //PointerMapName = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + mapName), 0x700L);
                PointerHP = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + hitPoints), 0xA0L, 0xD0L, 0x70L);
                //PointerEnemyEntryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + enemyHitPoints), 0x190L, 0x70L);
                
                //PointerBagCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + bagCount));
                //PointerInventoryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L);
                //PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + selectedSlot), 0x240L, 0x58L, 0x228L);
                //PointerItemNames = new MultilevelPointer[24];
                //PointerItemInfo = new MultilevelPointer[24];
                //
                //PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                //GenerateEnemyEntries();
            }
        }

        //private void GenerateEnemyEntries()
        //{
        //    long position;
        //    // Loop through and create all of the pointers for the table.
        //    for (long i = 0; i < PointerEnemyEntries.Length; ++i)
        //    {
        //        position = 0x0L + (i * 0x08L);
        //        PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + enemyHitPoints), 0x58L, 0xB0L, 0x70L, 0x20L, position, 0x70L);
        //    }
        //}

        //private void GetItems()
        //{
        //    if (gameMemoryValues.PlayerInventory == null)
        //    {
        //        gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
        //    }
        //    if (gameMemoryValues.PlayerInventoryCount != 0)
        //    {
        //        for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
        //        {
        //            long position = (0x30L + (0x8L * i));
        //            PointerItemNames[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L, 0x80L);
        //            PointerItemInfo[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L);
        //        }
        //        UpdateItems();
        //    }
        //}

        //private void UpdateItems()
        //{
        //    if (gameMemoryValues.PlayerInventoryCount != 0)
        //    {
        //        for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
        //        {
        //            PointerItemNames[i].UpdatePointers();
        //            PointerItemInfo[i].UpdatePointers();
        //        }
        //    }
        //    RefreshItems();
        //}

        //private void RefreshItems()
        //{
        //    if (gameMemoryValues.PlayerInventoryCount != 0)
        //    {
        //        for (int i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
        //        {
        //            gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
        //        }
        //        for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
        //        {
        //            var length = PointerItemNames[i].DerefInt(0x20);
        //            if (length > 0)
        //            {
        //                var bytes = PointerItemNames[i].DerefByteArray(0x24, length * 2);
        //                gameMemoryValues.PlayerInventory[i].SetValues(PointerItemInfo[i].DerefByte(0xB0), System.Text.Encoding.Unicode.GetString(bytes), PointerItemInfo[i].DerefInt(0x88));
        //            }
        //        }
        //    }
        //}

        private void SelectPointerAddresses(GameVersion version)
        {
            if (version == GameVersion.STEAM)
            {
                difficultyAdjustment = 0x081FA818;
                //selectedSlot = 0x081F2620;
                //itemCount = 0x081F1308;
                hitPoints = 0x081EA150;
                //enemyHitPoints = 0x081E9A98;
                //mapName = 0x081E9B00;
                //bagCount = 0x081EA150;
                Console.WriteLine("Steam Version Detected!");
            }
            else if (version == GameVersion.WINDOWS){
                difficultyAdjustment = 0x0933E618;
                //selectedSlot = 0x09336170;
                //itemCount = 0x093352C0;
                hitPoints = 0x9373DB8;
                //enemyHitPoints = 0x09417178;
                //mapName = 0x0932F7E8;
                //bagCount = 0x09373DB8;
                Console.WriteLine("Microsoft Store Version Detected!");
            } 
            else
            {
                Console.WriteLine("Warning Unknown Version Will Not Work!");
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void UpdatePointers()
        {
            PointerDA.UpdatePointers();
            //PointerMapName.UpdatePointers();
            PointerHP.UpdatePointers();
            //PointerBagCount.UpdatePointers();
            //PointerInventoryCount.UpdatePointers();
            //PointerInventorySlotSelected.UpdatePointers();
            //PointerEnemyEntryCount.UpdatePointers();
            //GenerateEnemyEntries(); // This has to be here for the next part.
            //for (int i = 0; i < PointerEnemyEntries.Length; ++i)
            //{
            //    PointerEnemyEntries[i].UpdatePointers();
            //}
        }

        internal IGameMemoryRE7 Refresh()
        {
            //GetMap();
            gameMemoryValues._rankScore = PointerDA.DerefFloat(0xF8);
            gameMemoryValues._player = PointerHP.Deref<GamePlayer>(0x20);
            //GetBagCount();
            //gameMemoryValues._playerInventoryCount = PointerInventoryCount.DerefInt(0x28);
            //GetEnemies();
            //GetSelectedIndex();
            //GetItems();
            HasScanned = true;
            return gameMemoryValues;
        }

        //private void GetSelectedIndex()
        //{
        //    if (PointerInventorySlotSelected.Address != IntPtr.Zero)
        //    {
        //        gameMemoryValues._playerCurrentSelectedInventorySlots = PointerInventorySlotSelected.DerefInt(0x24);
        //    }
        //    else
        //    {
        //        gameMemoryValues._playerCurrentSelectedInventorySlots = -1;
        //    }
        //}

        //private void GetEnemies()
        //{
        //    GenerateEnemyEntries();
        //    if (gameMemoryValues.EnemyHealth == null)
        //    {
        //        gameMemoryValues.EnemyHealth = new EnemyHP[MAX_ENTITIES];
        //        for (int i = 0; i < MAX_ENTITIES; ++i)
        //            gameMemoryValues.EnemyHealth[i] = new EnemyHP();
        //    }
        //    for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
        //    {
        //        if (PointerEnemyEntries[i].Address != IntPtr.Zero)
        //        {
        //                GamePlayer enemyHP = PointerEnemyEntries[i].Deref<GamePlayer>(0x20);
        //                gameMemoryValues.EnemyHealth[i]._maximumHP = enemyHP.MaxHP;
        //                gameMemoryValues.EnemyHealth[i]._currentHP = enemyHP.CurrentHP;
        //        }
        //        else
        //        {
        //            gameMemoryValues.EnemyHealth[i]._maximumHP = 0;
        //            gameMemoryValues.EnemyHealth[i]._currentHP = 0;
        //        }
        //    }
        //}

        //private void GetBagCount()
        //{
        //    if (PointerBagCount.Address != IntPtr.Zero)
        //    {
        //        gameMemoryValues._playerInventorySlots = (PointerBagCount.DerefInt(0x78) * 4) + 12;
        //    }
        //    else
        //    {
        //        gameMemoryValues._playerInventorySlots = 12;
        //    }
        //}

        //private void GetMap()
        //{
        //    try
        //    {
        //        byte[] bytes = new byte[0];
        //        try { bytes = PointerMapName.DerefByteArray(0x0, 64); }
        //        catch { gameMemoryValues._mapName = "None"; }
        //
        //        int length = 0;
        //        for (var i = 0; i < bytes.Length; i += 2)
        //        {
        //            if (bytes[i] == 0x00 && bytes[i + 1] == 0x00)
        //            {
        //                length = i;
        //                break;
        //            }
        //        }
        //        if (length == 0) { 
        //            gameMemoryValues._mapName = "None";
        //        }
        //        else
        //        {
        //            gameMemoryValues._mapName = System.Text.Encoding.Unicode.GetString(bytes, 0, length);
        //        }
        //    }
        //    catch
        //    {
        //        gameMemoryValues._mapName = "None";
        //    }
        //}

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (memoryAccess != null)
                        memoryAccess.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~REmake1Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
#endregion
    }
}
