using SRTPluginBase;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SRTPluginProviderRE7
{
    public class SRTPluginProviderRE7 : IPluginProvider
    {
        private GameMemoryRE7Scanner gameMemoryScanner;
        private IPluginHostDelegates hostDelegates;
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
                        gameMemoryScanner.Initialize(gameProcess);
                }
                return gameMemoryScanner != null && gameMemoryScanner.ProcessRunning;
            }
        }
        public IPluginInfo Info => new PluginInfo();

        private static readonly byte[] re7steam_WW_20220614_1 = new byte[32] { 0x13, 0x8F, 0xDF, 0x58, 0x49, 0x37, 0x47, 0xDF, 0xB8, 0xA1, 0x82, 0x43, 0x25, 0x2B, 0x0F, 0x61, 0x58, 0x92, 0xC4, 0xD0, 0x10, 0xD9, 0x1C, 0x8E, 0x9E, 0xAF, 0x9B, 0x86, 0x38, 0xFA, 0x58, 0x02 };
        private static readonly byte[] re7steam_ww_20211217_1 = new byte[32] { 0xB4, 0xF6, 0x58, 0x5F, 0xC0, 0xB6, 0xD0, 0x67, 0x74, 0x59, 0xB1, 0x6C, 0x75, 0xD6, 0x6B, 0x3E, 0x85, 0xAD, 0x23, 0x93, 0x87, 0xCC, 0x89, 0xD1, 0x08, 0xC0, 0xCA, 0x77, 0x93, 0x29, 0x6E, 0xCD };

        public int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            gameProcess = Process.GetProcessesByName("re7").FirstOrDefault();
            if (gameProcess != default)
            {
                Console.WriteLine($"Game process found: {gameProcess.ProcessName} (PID: {gameProcess.Id})");
                string? filePath = gameProcess?.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine($"Game located at \"{filePath}\"");
                    gameMemoryScanner = new GameMemoryRE7Scanner(gameProcess);
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                }
                return 0;
            }
            else
            {
                return 0;
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

                return gameMemoryScanner.Refresh();
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
