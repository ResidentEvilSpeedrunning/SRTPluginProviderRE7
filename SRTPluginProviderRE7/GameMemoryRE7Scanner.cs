using ProcessMemory;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using SRTPluginProviderRE7.Structs;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SRTPluginProviderRE7
{
    internal class GameMemoryRE7Scanner : IDisposable
    {
        // Variables
        private ProcessMemoryHandler memoryAccess;
        private GameMemoryRE7 gameMemoryValues;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;
        private int EnemyTableCount;
        private int JackTableCount;

        // Pointer Address Variables
        private long difficultyAdjustment;
        private long hitPoints;
        private long enemyHitPoints;
        private long jackHitPoints;
        private long selectedSlot;
        private long itemCount;
        private long mapName;
        private long bagCount;
        private long stats;
        private long coins;
        private long timer;
        private long gameState;
        private long gameplayState;
        private long gameInit;

        private bool connected;
        private bool isReset;

        // Pointer Classes
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerDA { get; set; }
        private MultilevelPointer PointerMapName { get; set; }
        private MultilevelPointer PointerHP { get; set; }
        private MultilevelPointer PointerBagCount { get; set; }
        private MultilevelPointer PointerInventoryCount { get; set; }
        private MultilevelPointer PointerInventorySlotSelected { get; set; }
        private MultilevelPointer PointerEnemyEntryCount { get; set; }
        private MultilevelPointer[] PointerEnemyEntries { get; set; }
        private MultilevelPointer[] PointerJackEntries { get; set; }
        private MultilevelPointer[] PointerItemNames { get; set; }
        private MultilevelPointer[] PointerItemInfo { get; set; }

        private MultilevelPointer PointerMrEverythingCount { get; set; }
        private MultilevelPointer PointerFileCount { get; set; }
        private MultilevelPointer PointerCoinCount { get; set; }
        private MultilevelPointer PointerTimer { get; set; }
        private MultilevelPointer PointerGameState { get; set; }
        private MultilevelPointer PointerGameplayState { get; set; }
        private MultilevelPointer PointerGameInit { get; set; }

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
                connected = true;
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't.

                PointerGameInit = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + gameInit));
                PointerGameplayState = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + gameplayState), 0x60L, 0x1A8L);
                PointerGameState = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + gameState), 0x28L, 0x428L, 0x40L, 0x28L);

                PointerMrEverythingCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + stats), 0x78L, 0x1F0L);
                PointerFileCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + stats), 0x60L, 0x198L, 0x20L);
                PointerCoinCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + coins), 0x88L, 0x40L, 0x80L);
                PointerDA = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + difficultyAdjustment));
                PointerMapName = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + mapName), 0x700L);
                PointerHP = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + hitPoints), 0xA0L, 0xD0L, 0x70L);
                //PointerHP = new MultilevelPointer(memoryAccess, BaseAddress + hitPoints, 0xA0L, 0xD0L, 0x70L);
                PointerEnemyEntryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + enemyHitPoints), 0x190L, 0x70L);
                GenerateEnemyEntries();
                GenerateJackEntries();
                PointerBagCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + bagCount));
                PointerInventoryCount = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L);
                PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + selectedSlot), 0x240L, 0x58L, 0x228L);
                PointerItemNames = new MultilevelPointer[32];
                PointerItemInfo = new MultilevelPointer[32];
                PointerTimer = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + timer));
            }
        }

        private void GenerateEnemyEntries()
        {
            EnemyTableCount = PointerEnemyEntryCount.DerefInt(0x820); // Get the size of the enemy pointer table. This seems to double (4, 8, 16, 32, ...) but never decreases, even after a new game is started.
            if (PointerEnemyEntries == null || PointerEnemyEntries.Length != EnemyTableCount) // Enter if the pointer table is null (first run) or the size does not match.
            {
                long position;
                if (EnemyTableCount > 0)
                {
                    PointerEnemyEntries = new MultilevelPointer[EnemyTableCount]; // Create a new enemy pointer table array with the detected size.
                }
                else
                {
                    PointerEnemyEntries = new MultilevelPointer[EnemyTableCount];
                }
                
                
                // Loop through and create all of the pointers for the table.
                for (long i = 0; i < PointerEnemyEntries.Length; ++i)
                {
                    position = 0x0L + (i * 0x08L);
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + enemyHitPoints), 0x58L, 0xB0L, 0x70L, 0x20L, position, 0x70L);
                }
            }
        }
        private void GenerateJackEntries()
        {
            JackTableCount = 8; // Get the size of the enemy pointer table. This seems to double (4, 8, 16, 32, ...) but never decreases, even after a new game is started.
            if (PointerJackEntries == null || PointerJackEntries.Length != JackTableCount) // Enter if the pointer table is null (first run) or the size does not match.
            {
                long position;
                if (JackTableCount > 0)
                {
                    PointerJackEntries = new MultilevelPointer[JackTableCount]; // Create a new enemy pointer table array with the detected size.
                }
                else
                {
                    PointerJackEntries = new MultilevelPointer[JackTableCount];
                }


                // Loop through and create all of the pointers for the table.
                for (long i = 0; i < PointerJackEntries.Length; ++i)
                {
                    position = 0x30L + (i * 0x08L);
                    PointerJackEntries[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + jackHitPoints), 0x40L, 0x40L, 0x38L, 0x110L, 0x90L, 0x20L, position);
                }
            }
        }

        private void GetItems()
        {
            if (gameMemoryValues.PlayerInventory == null)
            {
                gameMemoryValues.PlayerInventory = new InventoryEntry[24];
            }
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    long position = (0x30L + (0x8L * i));
                    PointerItemNames[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L, 0x80L);
                    PointerItemInfo[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + itemCount), 0x60L, 0x20L, position, 0x28L);
                }
                UpdateItems();
            }
        }

        private void UpdateItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    PointerItemNames[i].UpdatePointers();
                    PointerItemInfo[i].UpdatePointers();
                }
            }
            RefreshItems();
        }

        private void RefreshItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (int i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                {
                    gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                }
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    var length = PointerItemNames[i].DerefInt(0x20);
                    if (length > 0)
                    {
                        var bytes = PointerItemNames[i].DerefByteArray(0x24, length * 2);
                        gameMemoryValues.PlayerInventory[i].SetValues(PointerItemInfo[i].DerefByte(0xB0), System.Text.Encoding.Unicode.GetString(bytes), PointerItemInfo[i].DerefInt(0x88));
                    }
                }
            }
        }

        private void SelectPointerAddresses(GameVersion version)
        {
            if (version == GameVersion.STEAM)
            {
                difficultyAdjustment = 0x081FA818;
                selectedSlot = 0x081F2620;
                itemCount = 0x081F1308;
                hitPoints = 0x081EB330;
                enemyHitPoints = 0x081E9A98;
                jackHitPoints = 0x081EBBCB;
                mapName = 0x081E9B00;
                bagCount = 0x081EA150;
                stats = 0x081F65E8;
                coins = 0x08100000;
                timer = 0x08100000;
                gameState = 0x08100000;
                gameplayState = 0x08100000;
                gameInit = 0x08100000;
                Console.WriteLine("Steam Version Detected!");
            }
            else if (version == GameVersion.WINDOWS){
                difficultyAdjustment = 0x0933E618;
                selectedSlot = 0x09336170;
                itemCount = 0x093352C0;
                hitPoints = 0x9373DB8;
                enemyHitPoints = 0x09417178;
                jackHitPoints = 0x0933F138;
                mapName = 0x0932F7E8;
                bagCount = 0x09373DB8;
                stats = 0x0933A378;
                coins = 0x0933B380;
                timer = 0x0932F960;
                gameState = 0x093698F0;
                gameplayState = 0x0932F940;
                gameInit = 0x09369F28;
                Console.WriteLine("Microsoft Store Version Detected!");
            } 
            else
            {
                Console.WriteLine("Warning Unknown Version Might Not Work!");
                return;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal void UpdatePointers()
        {
            if (Process.GetProcessesByName("re7").FirstOrDefault() == null) { return; }
            if (!connected) { Initialize(Process.GetProcessesByName("re7").FirstOrDefault().Id); return; }
            else
            {
                PointerGameInit.UpdatePointers();
                PointerGameplayState.UpdatePointers();
                PointerGameState.UpdatePointers();
                PointerMrEverythingCount.UpdatePointers();
                PointerFileCount.UpdatePointers();
                PointerCoinCount.UpdatePointers();
                PointerDA.UpdatePointers();
                PointerMapName.UpdatePointers();
                PointerHP.UpdatePointers();
                PointerBagCount.UpdatePointers();
                PointerInventoryCount.UpdatePointers();
                PointerInventorySlotSelected.UpdatePointers();
                PointerEnemyEntryCount.UpdatePointers();
                GenerateEnemyEntries(); // This has to be here for the next part.
                for (int i = 0; i < PointerEnemyEntries.Length; ++i)
                {
                    PointerEnemyEntries[i].UpdatePointers();
                }
                GenerateJackEntries();
                for (int i = 0; i < PointerJackEntries.Length; ++i)
                {
                    PointerJackEntries[i].UpdatePointers();
                }
                PointerTimer.UpdatePointers();
            }
        }

        internal IGameMemoryRE7 Refresh()
        {
            gameMemoryValues.GameInit = PointerGameInit.DerefInt(0x38);
            gameMemoryValues.GameplayState = PointerGameplayState.DerefInt(0x83C);
            gameMemoryValues.GameState = PointerGameState.DerefInt(0x104);
            GetMap();
            gameMemoryValues.MrEverything = PointerMrEverythingCount.DerefInt(0x28);
            CheckRestart();
            gameMemoryValues.FileCount = PointerFileCount.DerefInt(0x28);
            gameMemoryValues.CoinCount = PointerCoinCount.DerefInt(0x20);
            gameMemoryValues.CurrentDA = PointerDA.DerefFloat(0xF8);
            GetHPValues();
            GetBagCount();
            gameMemoryValues.PlayerInventoryCount = PointerInventoryCount.DerefInt(0x28);
            GetEnemies();
            GetSelectedIndex();
            GetItems();
            GetJack();
            gameMemoryValues.Timestamp = PointerTimer.DerefLong(0x358);
            HasScanned = true;
            return gameMemoryValues;
        }

        private void GetHPValues()
        {
            if (PointerHP.Address != IntPtr.Zero)
            {
                gameMemoryValues.CurrentHP = PointerHP.DerefFloat(0x24);
                gameMemoryValues.MaxHP = PointerHP.DerefFloat(0x20);
            }
            else
            {
                gameMemoryValues.CurrentHP = 0;
                gameMemoryValues.MaxHP = 0;
            }
        }

        private void GetSelectedIndex()
        {
            if (PointerInventorySlotSelected.Address != IntPtr.Zero)
            {
                gameMemoryValues.PlayerCurrentSelectedInventorySlots = PointerInventorySlotSelected.DerefInt(0x24);
            }
            else
            {
                gameMemoryValues.PlayerCurrentSelectedInventorySlots = -1;
            }
        }

        private void GetEnemies()
        {
            GenerateEnemyEntries();
            if (gameMemoryValues.EnemyHealth == null || gameMemoryValues.EnemyHealth.Length < EnemyTableCount)
            {
                gameMemoryValues.EnemyHealth = new EnemyHP[EnemyTableCount];
                gameMemoryValues.EnemyCount = gameMemoryValues.EnemyHealth.Length;
                for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
                    gameMemoryValues.EnemyHealth[i] = new EnemyHP();
            }
            for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
            {
                if (i < PointerEnemyEntries.Length && PointerEnemyEntries[i].Address != IntPtr.Zero)
                { // While we're within the size of the enemy table, set the values.
                    gameMemoryValues.EnemyHealth[i].ID = PointerEnemyEntries[i].DerefUShort(0x48);
                    gameMemoryValues.EnemyHealth[i].MaximumHP = PointerEnemyEntries[i].DerefFloat(0x20);
                    gameMemoryValues.EnemyHealth[i].CurrentHP = PointerEnemyEntries[i].DerefFloat(0x24);
                }
                else
                { // We're beyond the current size of the enemy table. It must have shrunk because it was larger before but for the sake of performance, we're not going to constantly recreate the array any time the size doesn't match. Just blank out the remaining array values.
                    gameMemoryValues.EnemyHealth[i].ID = 0;
                    gameMemoryValues.EnemyHealth[i].MaximumHP = 0;
                    gameMemoryValues.EnemyHealth[i].CurrentHP = 0;
                }
            }
        }

        private void GetJack()
        {
            if (gameMemoryValues.MapName.Contains("Boss2F") || gameMemoryValues.MapName.Contains("Boss1F"))
            {
                GenerateJackEntries();
                if (gameMemoryValues.JackEyeHealth == null || gameMemoryValues.JackEyeHealth.Length < JackTableCount)
                {
                    gameMemoryValues.JackEyeHealth = new JackEyeHP[JackTableCount];
                    for (int i = 0; i < gameMemoryValues.JackEyeHealth.Length; ++i)
                        gameMemoryValues.JackEyeHealth[i] = new JackEyeHP();
                }
                for (int i = 0; i < gameMemoryValues.JackEyeHealth.Length; ++i)
                {
                    if (i < PointerJackEntries.Length && PointerJackEntries[i].Address != IntPtr.Zero)
                    { // While we're within the size of the enemy table, set the values.
                        gameMemoryValues.JackEyeHealth[i].CurrentHP = PointerJackEntries[i].DerefFloat(0x20);
                    }
                    else
                    { // We're beyond the current size of the enemy table. It must have shrunk because it was larger before but for the sake of performance, we're not going to constantly recreate the array any time the size doesn't match. Just blank out the remaining array values.
                        gameMemoryValues.JackEyeHealth[i].CurrentHP = 0;
                    }
                }
            }   
        }

        private void GetBagCount()
        {
            if (PointerBagCount.Address != IntPtr.Zero)
            {
                gameMemoryValues.PlayerInventorySlots = (PointerBagCount.DerefInt(0x78) * 4) + 12;
            }
            else
            {
                gameMemoryValues.PlayerInventorySlots = 12;
            }
        }

        private void GetMap()
        {
            try
            {
                byte[] bytes = new byte[0];
                try { bytes = PointerMapName.DerefByteArray(0x0, 64); }
                catch { gameMemoryValues.MapName = "None"; }

                int length = 0;
                for (var i = 0; i < bytes.Length; i += 2)
                {
                    if (bytes[i] == 0x00 && bytes[i + 1] == 0x00)
                    {
                        length = i;
                        break;
                    }
                }
                if (length == 0) { 
                    gameMemoryValues.MapName = "None";
                }
                else
                {
                    gameMemoryValues.MapName = System.Text.Encoding.Unicode.GetString(bytes, 0, length);
                }
            }
            catch
            {
                gameMemoryValues.MapName = "None";
            }
        }

        private void CheckRestart()
        {
            if (gameMemoryValues.MapName == "c04_Ship3FInfirmaryPast" && isReset)
            {
                Console.WriteLine("New Game Started... Resetting");
                isReset = false;
            }
            if (gameMemoryValues.MapName == "snd_zone_forest_start" && gameMemoryValues.MrEverything > 0 && !isReset)
            {
                isReset = true;
                Console.WriteLine("Setting Mr.Everything total to 0");
                memoryAccess.SetIntAt(IntPtr.Add(PointerMrEverythingCount.Address, 0x28), 0);
            }
        }

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
