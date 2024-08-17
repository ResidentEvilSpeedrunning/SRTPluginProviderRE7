using ProcessMemory;
using System;
using System.Diagnostics;
using SRTPluginProviderRE7.Structs;
using SRTPluginProviderRE7.Structs.GameStructs;

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
        public uint ProcessExitCode => (memoryAccess != null) ? memoryAccess.ProcessExitCode : 0;

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

        internal unsafe void Initialize(Process process, GameVersion gv)
        {
            if (process == null)
                return;
            SelectPointerAddresses(GameHashes.DetectVersion(process.MainModule.FileName));

            uint pid = (uint?)GetProcessId(process) ?? 0;
            Console.WriteLine($"Game PID: {pid}");
            memoryAccess = new ProcessMemoryHandler(pid);

            if (ProcessRunning)
            {
                //BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, PInvoke.ListModules.LIST_MODULES_64BIT); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn'
                BaseAddress = process?.MainModule?.BaseAddress ?? IntPtr.Zero;
                if (gv == GameVersion.STEAM_December2021)
                {
                    Console.WriteLine("This is steamversion december 2021");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x68, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x700);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesDX11();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesDX11();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
                else if (gv == GameVersion.STEAM_June2022)
                {
                    Console.WriteLine("This is steam version june 2022");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x58, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x960);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

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
                else if (gv == GameVersion.STEAM_October2022)
                {
                    Console.WriteLine("This is steam version october 2022");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x58, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x960);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

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
                else if (gv == GameVersion.STEAM_DX11_EOL)
                {
                    Console.WriteLine("This is steam version DX 11 EOL");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x68, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x700);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesSteamDX11EOL();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesSteamDX11EOL();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
                else if (gv == GameVersion.STEAM_DX12_09_05_2023)
                {
                    Console.WriteLine("This is steam version DX 12 Update September 2023");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x58, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x960);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

                    gameMemoryValues.PlayerInventory = new InventoryEntry[MAX_ITEMS];
                    PointerItemNames = new MultilevelPointer[MAX_ITEMS];
                    PointerItemInfo = new MultilevelPointer[MAX_ITEMS];

                    // Loop through and create all of the pointers for the table.
                    gameMemoryValues._enemyHealth = new EnemyHP[MAX_ENTITIES];
                    for (int i = 0; i < MAX_ENTITIES; ++i)
                        gameMemoryValues._enemyHealth[i] = new EnemyHP();

                    GenerateEnemyEntriesSteamDX12_09_05_2023();

                    gameMemoryValues._jackHP = new JackEyeHP[MAX_JACKEYES];
                    for (int i = 0; i < MAX_JACKEYES; ++i)
                        gameMemoryValues._jackHP[i] = new JackEyeHP();

                    GenerateJackEyesSteamDX12_09_05_2023();

                    for (var i = 0; i < gameMemoryValues.PlayerInventory.Length; ++i)
                    {
                        gameMemoryValues.PlayerInventory[i] = new InventoryEntry();
                    }
                }
                else
                {
                    Console.WriteLine("This is Windows");
                    PointerDA = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressDifficultyAdjustment));
                    PointerHP = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressHP), 0xE8, 0x68, 0x68, 0x70);
                    PointerRoomID = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressRoomID), 0x700);

                    PointerBagCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressBagCount));
                    PointerInventoryCount = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressItemCount), 0x68, 0x28);
                    PointerInventorySlotSelected = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressSelectedSlot), 0x68, 0x28);

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

        //Enemy HP CeroD
        private unsafe void GenerateEnemyEntriesWindows()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x60, 0x70, 0x20, 0x0 + (i * 0x8), 0x70);
            }

        }

        //DX11 2021
        private unsafe void GenerateEnemyEntriesDX11()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x60, 0x70, 0x20, 0x0 + (i * 0x8), 0x70);
            }

        }

        //DX11 EOL
        private unsafe void GenerateEnemyEntriesSteamDX11EOL()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x60, 0x70, 0x20, 0x0 + (i * 0x8), 0x70);
            }

        }

        //Original DX12 Update
        private unsafe void GenerateEnemyEntriesSteam()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x78, 0x640, 0x10, 0x0 + (i * 0x8), 0x70);
            }

        }

        //DX12 Oct 2022
        private unsafe void GenerateEnemyEntriesSteamOctober2022()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x78, 0x640, 0x10, 0x0 + (i * 0x8), 0x70);
            }

        }

        //DX12 May 2023
        private unsafe void GenerateEnemyEntriesSteamDX12_09_05_2023()
        {
            if (PointerEnemyEntries == null)
            {
                PointerEnemyEntries = new MultilevelPointer[MAX_ENTITIES];
                for (int i = 0; i < MAX_ENTITIES; ++i)
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressEnemyHP), 0x78, 0x640, 0x10, 0x0 + (i * 0x8), 0x70);
            }

        }

        //Jack Eye HP CeroD
        private unsafe void GenerateJackEyesWindows()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x20, 0x40, 0x30, 0x328, 0x90, 0x20, 0x30 + (i * 0x8), 0x20);
                }
            }
        }

        //DX11 2021
        private unsafe void GenerateJackEyesDX11()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x20, 0x40, 0x30, 0x328, 0x90, 0x20, 0x30 + (i * 0x8), 0x20);
                }
            }
        }

        //DX11 EOL
        private unsafe void GenerateJackEyesSteamDX11EOL()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x20, 0x40, 0x30, 0x328, 0x90, 0x20, 0x30 + (i * 0x8), 0x20);
                }
            }
        }

        //Original DX12 Update
        private unsafe void GenerateJackEyesSteam()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x10, 0x30, 0x28, 0x110, 0x90, 0x10, 0x20 + (i * 0x8), 0x10);
                }
            }
        }

        //DX12 Oct 2022
        private unsafe void GenerateJackEyesSteamOctober2022()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x10, 0x30, 0x28, 0x110, 0x90, 0x10, 0x20 + (i * 0x8), 0x10);
                }
            }
        }

        //DX12 May 2023
        private unsafe void GenerateJackEyesSteamDX12_09_05_2023()
        {
            if (PointerJackEyeHPs == null)
            {
                PointerJackEyeHPs = new MultilevelPointer[MAX_JACKEYES];
                for (int i = 0; i < MAX_JACKEYES; ++i)
                {
                    PointerJackEyeHPs[i] = new MultilevelPointer(memoryAccess, (nint*)IntPtr.Add(BaseAddress, pointerAddressJackEyeHP), 0xB8, 0x10, 0x30, 0x28, 0x110, 0x90, 0x10, 0x20 + (i * 0x8), 0x10);
                }
            }
        }

        private unsafe void GetItems()
        {
            if (gameMemoryValues.PlayerInventoryCount != 0)
            {
                for (int i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    nint position = 0x30 + (0x8 * i);
                    PointerItemNames[i] = new MultilevelPointer(memoryAccess, (nint*)(BaseAddress + pointerAddressItemCount), 0x60, 0x20, position, 0x28, 0x80);
                    PointerItemInfo[i] = new MultilevelPointer(memoryAccess, (nint*)(BaseAddress + pointerAddressItemCount), 0x60, 0x20, position, 0x28);
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
                for (int i = 0; i < gameMemoryValues.PlayerInventoryCount; i++)
                {
                    int length = PointerItemNames[i].DerefInt(0x20);
                    if (length > 0)
                    {
                        byte[]? bytes = PointerItemNames[i].DerefByteArray(0x24, (nuint)(length * 2));
                        gameMemoryValues.PlayerInventory[i].SetValues(PointerItemInfo[i].DerefByte(0xB0), System.Text.Encoding.Unicode.GetString(bytes), PointerItemInfo[i].DerefInt(0x88));
                    }
                }
            }
        }

        private void SelectPointerAddresses(GameVersion version)
        {
            if (version == GameVersion.WINDOWS)
            {
                pointerAddressDifficultyAdjustment = 0x09387430;
                pointerAddressSelectedSlot = 0x0;
                pointerAddressItemCount = 0x0;
                pointerAddressHP = 0x934D678;
                pointerAddressEnemyHP = 0x934CDF8;
                pointerAddressBagCount = 0x0;
                pointerAddressJackEyeHP = 0x934D678;
                pointerAddressRoomID = 0x0934CCA0;
                Console.WriteLine("Microsoft Store Version Detected!");
            }
            else if (version == GameVersion.STEAM_December2021)
            {
                pointerAddressDifficultyAdjustment = 0x081FA818;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x822BE48;
                pointerAddressEnemyHP = 0x81E9C58;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x822BE48;
                pointerAddressRoomID = 0x0934A600;
                Console.WriteLine("Steam Version December 2021 Detected!");
            }
            else if (version == GameVersion.STEAM_DX11_EOL)
            {
                pointerAddressDifficultyAdjustment = 0x8207330;
                pointerAddressSelectedSlot = 0x0;
                pointerAddressItemCount = 0x0;
                pointerAddressHP = 0x8238CF0;
                pointerAddressEnemyHP = 0x81F7370;
                pointerAddressBagCount = 0x0;
                pointerAddressJackEyeHP = 0x8238CF0;
                pointerAddressRoomID = 0x081F7218;
                Console.WriteLine("Steam Version DX11 End of Life Detected!");
            }
            else if (version == GameVersion.STEAM_June2022)
            {
                pointerAddressDifficultyAdjustment = 0x08FC42F8;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x8F86B30;
                pointerAddressEnemyHP = 0x8F7E0E0;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x8F86B30;
                pointerAddressRoomID = 0x08F7DE00;
                Console.WriteLine("Steam Version June 2022 Detected!");
            }
            else if (version == GameVersion.STEAM_October2022)
            {
                pointerAddressDifficultyAdjustment = 0x8FC4478;
                pointerAddressSelectedSlot = 0x081F2620;
                pointerAddressItemCount = 0x081F1308;
                pointerAddressHP = 0x8F86CB0;
                pointerAddressEnemyHP = 0x8F7E260;
                pointerAddressBagCount = 0x081EA150;
                pointerAddressJackEyeHP = 0x8F86CB0;
                pointerAddressRoomID = 0x08F7DF80;
                Console.WriteLine("Steam Version October 2022 Detected!");
            }
            else if (version == GameVersion.STEAM_DX12_09_05_2023)
            {
                pointerAddressDifficultyAdjustment = 0x8FF2790;
                pointerAddressSelectedSlot = 0x0;
                pointerAddressItemCount = 0x0;
                pointerAddressHP = 0x8FB4BE0;
                pointerAddressEnemyHP = 0x8FAC390;
                pointerAddressBagCount = 0x0;
                pointerAddressJackEyeHP = 0x8FB4BE0;
                pointerAddressRoomID = 0x8FAC0B0;
                Console.WriteLine("Steam Version DX12 Detected!");
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
            if (gv == GameVersion.STEAM_June2022 || gv == GameVersion.STEAM_October2022 || gv == GameVersion.STEAM_DX12_09_05_2023)
            {
                GetEnemiesSteam();
                GetJackEyesSteam();
                gameMemoryValues._player = PointerHP.Deref<GamePlayer>(0x10);
            }
            else if (gv == GameVersion.WINDOWS || gv == GameVersion.STEAM_December2021 || gv == GameVersion.STEAM_DX11_EOL)
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
            for (int i = 0; i < gameMemoryValues.JackHP.Length; ++i)
            {
                if (PointerJackEyeHPs[i].Address != IntPtr.Zero)
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
