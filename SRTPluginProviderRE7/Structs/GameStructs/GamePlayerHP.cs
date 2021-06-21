using System.Runtime.InteropServices;

namespace SRTPluginProviderRE7.Structs.GameStructs
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]

    public unsafe struct GamePlayerHP
    {
        [FieldOffset(0x20)] public float Max;
        [FieldOffset(0x24)] public float Current;

        public static GamePlayerHP AsStruct(byte[] data)
        {
            fixed (byte* pb = &data[0])
            {
                return *(GamePlayerHP*)pb;
            }
        }
    }
}