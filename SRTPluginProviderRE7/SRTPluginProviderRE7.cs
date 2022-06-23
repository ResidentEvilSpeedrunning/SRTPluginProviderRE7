using SRTPluginBase;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Security.Cryptography;

namespace SRTPluginProviderRE7
{
    public class SRTPluginProviderRE7 : IPluginProvider
    {
        private GameMemoryRE7Scanner gameMemoryScanner;
        private IPluginHostDelegates hostDelegates;
        private GameVersion gameVersion;
        private Process? gameProcess;
        private Stopwatch stopwatch;
        public bool GameRunning
        {
            get
            {
                if (gameMemoryScanner != null && !gameMemoryScanner.ProcessRunning)
                {
                    gameProcess = GetProcess();
                    if (gameProcess != null)
                        gameMemoryScanner.Initialize(gameProcess, gameVersion);
                }
                return gameMemoryScanner != null && gameMemoryScanner.ProcessRunning;
            }
        }
        public IPluginInfo Info => new PluginInfo();

        private static readonly byte[] re7steam_WW_20220614_1 = new byte[32] { 0x13, 0x8F, 0xDF, 0x58, 0x49, 0x37, 0x47, 0xDF, 0xB8, 0xA1, 0x82, 0x43, 0x25, 0x2B, 0x0F, 0x61, 0x58, 0x92, 0xC4, 0xD0, 0x10, 0xD9, 0x1C, 0x8E, 0x9E, 0xAF, 0x9B, 0x86, 0x38, 0xFA, 0x58, 0x02 };

        public int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            gameProcess = Process.GetProcessesByName("re7").FirstOrDefault();
            if (gameProcess != default)
            {
                string? filePath = gameProcess?.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    // detect file version now that we have the path to the exe, whether it is via checksum or if it has 'WindowsApps' in its path.
                    // e.g

                    if (filePath.Contains("WindowsApps"))
                        gameVersion = GameVersion.WINDOWS;
                    else
                    {
                        byte[] checksum;
                        using (SHA256 hashFunc = SHA256.Create())
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            checksum = hashFunc.ComputeHash(fs);
                        if (checksum.SequenceEqual(re7steam_WW_20220614_1))
                            gameVersion = GameVersion.STEAM_June2022;
                        else
                            gameVersion = GameVersion.STEAM_December2021;
                    }
                    gameMemoryScanner = new GameMemoryRE7Scanner(gameProcess, gameVersion);
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }
                return 0;
            } 
            else
            {
                return 1;
            }
            
        }

        public int Shutdown()
        {
            // Clean up here (disposes, nulling large objects, etc)
            gameMemoryScanner?.Dispose();
            gameMemoryScanner = null;
            stopwatch?.Stop();
            stopwatch = null;
            return 0;
        }

        public object PullData()
        {
            try
            {
                if (!GameRunning) // Not running? Bail out!
                    return null;

                if (stopwatch.ElapsedMilliseconds >= 2000L)
                {
                    gameMemoryScanner.UpdatePointers();
                    stopwatch.Restart();
                }

                return gameMemoryScanner.Refresh(gameVersion);
            }
            catch (Win32Exception ex)
            {
                if ((ProcessMemory.Win32Error)ex.NativeErrorCode != ProcessMemory.Win32Error.ERROR_PARTIAL_COPY)
                    hostDelegates.ExceptionMessage(ex);// Only show the error if its not ERROR_PARTIAL_COPY. ERROR_PARTIAL_COPY is typically an issue with reading as the program exits or reading right as the pointers are changing (i.e. switching back to main menu).
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage(ex);
            }

            return null;
        }
        private Process GetProcess() => Process.GetProcessesByName("re7")?.FirstOrDefault();
    }
}
