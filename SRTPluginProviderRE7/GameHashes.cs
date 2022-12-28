using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SRTPluginProviderRE7
{
    /// <summary>
    /// SHA256 hashes for the RE3/BIO3 REmake game executables.
    /// 
    /// Resident Evil 3 (WW): https://steamdb.info/app/952060/ / https://steamdb.info/depot/952062/
    /// Biohazard 3 (CERO Z): https://steamdb.info/app/1100830/ / https://steamdb.info/depot/1100831/
    /// </summary>
    /// 

    public enum GameVersion : int
    {
        STEAM_June2022,
        STEAM_December2021,
        STEAM_October2022,
        WINDOWS,
        UNKNOWN
    }

    public static class GameHashes
    {
        private static readonly byte[] re7steam_WW_20220614_1 = new byte[32] { 0x13, 0x8F, 0xDF, 0x58, 0x49, 0x37, 0x47, 0xDF, 0xB8, 0xA1, 0x82, 0x43, 0x25, 0x2B, 0x0F, 0x61, 0x58, 0x92, 0xC4, 0xD0, 0x10, 0xD9, 0x1C, 0x8E, 0x9E, 0xAF, 0x9B, 0x86, 0x38, 0xFA, 0x58, 0x02 };
        private static readonly byte[] re7steam_ww_20211217_1 = new byte[32] { 0xB4, 0xF6, 0x58, 0x5F, 0xC0, 0xB6, 0xD0, 0x67, 0x74, 0x59, 0xB1, 0x6C, 0x75, 0xD6, 0x6B, 0x3E, 0x85, 0xAD, 0x23, 0x93, 0x87, 0xCC, 0x89, 0xD1, 0x08, 0xC0, 0xCA, 0x77, 0x93, 0x29, 0x6E, 0xCD };
        private static readonly byte[] re7steam_ww_20221007_1 = new byte[32] { 0x8F, 0xA9, 0x6C, 0xDC, 0x83, 0x79, 0xAA, 0x2D, 0x8E, 0xE3, 0xCD, 0xD0, 0xD6, 0xEA, 0xAE, 0x1E, 0xAC, 0x4B, 0xEB, 0x15, 0x38, 0xEF, 0x5E, 0xE6, 0x62, 0x56, 0xDA, 0x3E, 0x92, 0x3D, 0xE3, 0x6D };


        public static GameVersion DetectVersion(string filePath)
        {
            if (filePath.Contains("Windows"))
            {
                return GameVersion.WINDOWS;
            }

            byte[] checksum;
            using (SHA256 hashFunc = SHA256.Create())
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                checksum = hashFunc.ComputeHash(fs);

            if (checksum.SequenceEqual(re7steam_ww_20211217_1))
                return GameVersion.STEAM_December2021;
            else if (checksum.SequenceEqual(re7steam_WW_20220614_1))
                return GameVersion.STEAM_June2022;
            else if (checksum.SequenceEqual(re7steam_ww_20221007_1))
                return GameVersion.STEAM_October2022;
            else
                return GameVersion.UNKNOWN;
        }
    }
}