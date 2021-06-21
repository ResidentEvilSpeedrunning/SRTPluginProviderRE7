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

    public enum GameVersion 
    {
        STEAM,
        WINDOWS,
        UNKNOWN
    }

    public static class GameHashes
    {
        private static readonly byte[] re7steam_ww_20210619_1 = new byte[32] { 0x1F, 0xD2, 0x43, 0xCB, 0x66, 0x35, 0xCF, 0x52, 0x1A, 0xC5, 0xF1, 0xA7, 0x41, 0xF7, 0x82, 0xE2, 0x4F, 0x6D, 0xD9, 0x55, 0x7C, 0x8A, 0xC5, 0x51, 0x23, 0x8C, 0xBB, 0x60, 0x5F, 0x13, 0x85, 0x84 };

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

            if (checksum.SequenceEqual(re7steam_ww_20210619_1))
                return GameVersion.STEAM;
            else
                return GameVersion.UNKNOWN;
        }
    }
}