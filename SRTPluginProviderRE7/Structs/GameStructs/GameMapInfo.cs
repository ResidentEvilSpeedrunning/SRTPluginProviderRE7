using System;
using System.Runtime.InteropServices;

namespace SRTPluginProviderRE7.Structs.GameStructs
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x740)] // SET DEFAULT SIZE OF 64 INCASE
    public unsafe struct GameMapInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)][FieldOffset(0x700)] private char* name;
        public string Name => new string(name);
    }
}
