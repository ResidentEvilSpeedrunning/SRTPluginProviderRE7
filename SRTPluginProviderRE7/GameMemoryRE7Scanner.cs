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
        private static readonly int MAX_JACKEYES = 8;

        // Variables
        private ProcessMemoryHandler memoryAccess;
        private GameMemoryRE7 gameMemoryValues;
        public bool HasScanned;
        public bool ProcessRunning => memoryAccess != null && memoryAccess.ProcessRunning;
        public int ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;

        // Pointer Address Variables
        private int pointerAddressDifficultyAdjustment;
        private int pointerAddressHP;
        private int pointerAddressEnemyHP;
        private int pointerAddressSelectedSlot;
        private int pointerAddressItemCount;
        private int pointerAddressBagCount;
        private int pointerAddressJackEyeHP;
        private int pointerAddressRoomID;

        // Pointer Classes
        private IntPtr BaseAddress { get; set; }
        private MultilevelPointer PointerDA { get; set; }
        private MultilevelPointer PointerHP { get; set; }
        private MultilevelPointer PointerBagCount { get; set; }
        private MultilevelPointer PointerInventoryCount { get; set; }
        private MultilevelPointer PointerInventorySlotSelected { get; set; }
        private MultilevelPointer PointerRoomID { get; set; }
        private MultilevelPointer[] PointerEnemyEntries { get; set; }
        private MultilevelPointer[] PointerJackEyeHPs { get; set; }
        private MultilevelPointer[] PointerItemNames { get; set; }
        private MultilevelPointer[] PointerItemInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        internal GameMemoryRE7Scanner(Process process, GameVersion gv)
        {
            gameMemoryValues = new GameMemoryRE7();
            if (process != null)
                Initialize(process, gv);
        }

        internal void Initialize(Process process, GameVersion gv)
        {
            if (process == null)
                return;
            SelectPointerAddresses(GameHashes.DetectVersion(process.MainModule.FileName));

            int pid = GetProcessId(process).Value;
            memoryAccess = new ProcessMemoryHandler(pid);

            if (ProcessRunning)
            {
                BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn'
                if (gv == GameVersion.STEAM_December2021)
                {
                    Console.WriteLine("This is steamversion december 2021");
                    PointerDA = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressHP), 0x2C0, 0x38, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x700);

                    PointerBagCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesWindows();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesWindows();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
                else if(gv == GameVersion.STEAM_June2022)
                {
                    Console.WriteLine("This is steam version june 2022");
                    PointerDA = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x960);

                    PointerBagCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesSteam();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesSteam();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                } 
                else if(gv == GameVersion.STEAM_October2022)
                {
                    Console.WriteLine("This is steam version october 2022");
                    PointerDA = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressHP), 0x2C0, 0x30, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x960);

                    PointerBagCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesSteamOctober2022();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesSteamOctober2022();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
                else
                {
                    Console.WriteLine("This is Windows");
                    PointerDA = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressHP), 0x2C0, 0x38, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x700);

                    PointerBagCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesWindows();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesWindows();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
            }
        }
        private unsafe void GenerateEnemyEntriesWindows()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x190, 0x490, 0x20, 0x8 + (i * 0x8), 0x58, 0x70);
            }

        }
        private unsafe void GenerateEnemyEntriesSteam()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x40 + (i * 0x8), 0x58, 0x70);
            }

        }
        private unsafe void GenerateEnemyEntriesSteamOctober2022()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x20, 0x28 + (i * 0x8), 0x48, 0xD8, 0x70);
            }

        }
        private unsafe void GenerateJackEyesWindows()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0x40, 0x30, 0xB8, 0x110, 0x90, 0x20, 0x30 + (i * 0x8));
                }
            }
        }

        private unsafe void GenerateJackEyesSteam()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0x7B8, 0x28, 0x18, 0x728, 0x90, 0x48, 0xA0 + (i * 0x8));
                }
            }
        }

        private unsafe void GenerateJackEyesSteamOctober2022()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0x20, 0x30, 0x20, 0x328, 0x90, 0x10, 0x20 + (i * 0x8));
                }
            }
        }

        private void GetItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (var i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    long position = (0x30L + (0x8L * i));
                    PointerItemNames[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + pointerAddressItemCount), 0x60L, 0x20L, position, 0x28L, 0x80L);
                    PointerItemInfo[i] = new MultilevelPointer(memoryAccess, (IntPtr)(BaseAddress + pointerAddressItemCount), 0x60L, 0x20L, position, 0x28L);
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
            if (version == GameVersion.STEAM_December2021)
            {
                pointerAddressDifficultyAdjustment = 0x081FA818;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x081EA150;
                pointerAddressEnemyHP = 0x081E9A98;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x093881A0;
                pointerAddressRoomID = 0x0934A600;
                Console.WriteLine("Steam Version December 2021 Detected!");
            }
            else if(version == GameVersion.STEAM_June2022)
            {
                pointerAddressDifficultyAdjustment = 0x08FC42F8;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x08F8D9A8;
                pointerAddressEnemyHP = 0x08F8BE68;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x08FBA528;
                pointerAddressRoomID = 0x08F7DE00;
                Console.WriteLine("Steam Version June 2022 Detected!");
            }
            else if(version == GameVersion.STEAM_October2022)
            {
                pointerAddressDifficultyAdjustment = 0x8FC4478;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x08FC4478;
                pointerAddressEnemyHP = 0x08FBA6A8;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x08FBA6A8;
                pointerAddressRoomID = 0x08F7DF80;
                Console.WriteLine("Steam Version October 2022 Detected!");
            }
            else if (version == GameVersion.WINDOWS){
                pointerAddressDifficultyAdjustment = 0x09384AB8;
                pointerAddressSelectedSlot = 0x09336170;
                pointerAddressItemCount = 0x093352C0;
                pointerAddressHP = 0x09384AB8;
                pointerAddressEnemyHP = 0x0934A598;
                pointerAddressBagCount = 0x09373DB8;
                pointerAddressJackEyeHP = 0x093881A0;
                pointerAddressRoomID = 0x0934A600;
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
            PointerHP.UpdatePointers();
            PointerBagCount.UpdatePointers();
            PointerInventoryCount.UpdatePointers();
            PointerInventorySlotSelected.UpdatePointers();
            PointerRoomID.UpdatePointers();
            for (int i = 0; i < PointerEnemyEntries.Length; ++i)
                PointerEnemyEntries[i].UpdatePointers();

            for (int i = 0; i < PointerJackEyeHPs.Length; ++i)
                PointerJackEyeHPs[i].UpdatePointers();
        }
        internal IGameMemoryRE7 Refresh(GameVersion gv)
        {
            if (gv == GameVersion.STEAM_June2022 || gv == GameVersion.STEAM_October2022)
            {
                GetEnemiesSteam();
                GetJackEyesSteam();
                gameMemoryValues._player = PointerHP.Deref<GamePlayer>(0x10);
            }
            else if (gv == GameVersion.WINDOWS || gv == GameVersion.STEAM_December2021)
            {
                GetEnemiesWindows();
                GetJackEyesWindows();
                gameMemoryValues._player = PointerHP.Deref<GamePlayer>(0x20);
            } 
            else
            {
                Console.WriteLine("No Version was recognized");
            }
            gameMemoryValues._roomID = PointerRoomID.DerefUnicodeString(0x0, 30);
            gameMemoryValues._rankScore = PointerDA.DerefFloat(0xF8);
            GetBagCount();
            gameMemoryValues._playerInventoryCount = PointerInventoryCount.DerefInt(0x28);
            GetSelectedIndex();
            GetItems();
            HasScanned = true;
            return gameMemoryValues;
        }


        private void GetSelectedIndex()
        {
            if (PointerInventorySlotSelected.Address != IntPtr.Zero)
            {
                gameMemoryValues._playerCurrentSelectedInventorySlots = PointerInventorySlotSelected.DerefInt(0x24);
                return;
            }
            gameMemoryValues._playerCurrentSelectedInventorySlots = 0;
        }

        private void GetEnemiesSteam()
        {
            for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
            {
                if (PointerEnemyEntries[i].Address != IntPtr.Zero)
                {
                    GamePlayer enemyHP = PointerEnemyEntries[i].Deref<GamePlayer>(0x10);
                    gameMemoryValues.EnemyHealth[i]._maximumHP = enemyHP.MaxHP;
                    gameMemoryValues.EnemyHealth[i]._currentHP = enemyHP.CurrentHP;
                }
                else
                {
                    gameMemoryValues.EnemyHealth[i]._maximumHP = 0;
                    gameMemoryValues.EnemyHealth[i]._currentHP = 0;
                }
            }
        }
        private void GetEnemiesWindows()
        {
            for (int i = 0; i < gameMemoryValues.EnemyHealth.Length; ++i)
            {
                if (PointerEnemyEntries[i].Address != IntPtr.Zero)
                {
                    GamePlayer enemyHP = PointerEnemyEntries[i].Deref<GamePlayer>(0x20);
                    gameMemoryValues.EnemyHealth[i]._maximumHP = enemyHP.MaxHP;
                    gameMemoryValues.EnemyHealth[i]._currentHP = enemyHP.CurrentHP;
                }
                else
                {
                    gameMemoryValues.EnemyHealth[i]._maximumHP = 0;
                    gameMemoryValues.EnemyHealth[i]._currentHP = 0;
                }
            }
        }

        private void GetJackEyesSteam()
        {
            for(int i = 0; i < gameMemoryValues.JackHP.Length; ++i)
            {
                if(PointerJackEyeHPs[i].Address != IntPtr.Zero)
                {
                    gameMemoryValues.JackHP[i]._currentHP = PointerJackEyeHPs[i].DerefFloat(0x10);
                } 
                else
                {
                    gameMemoryValues.JackHP[i]._currentHP = 0;
                }
            }
        }
        private void GetJackEyesWindows()
        {
            for (int i = 0; i < gameMemoryValues.JackHP.Length; ++i)
            {
                if (PointerJackEyeHPs[i].Address != IntPtr.Zero)
                {
                    gameMemoryValues.JackHP[i]._currentHP = PointerJackEyeHPs[i].DerefFloat(0x20);
                }
                else
                {
                    gameMemoryValues.JackHP[i]._currentHP = 0;
                }
            }
        }

        private void GetBagCount()
        {
            if (PointerBagCount.Address != IntPtr.Zero)
            {
                gameMemoryValues._playerInventorySlots = (PointerBagCount.DerefInt(0x78) * 4) + 12;
            }
            else
            {
                gameMemoryValues._playerInventorySlots = 12;
            }
        }

        private int? GetProcessId(Process process) => process?.Id;

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
