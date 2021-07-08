using System.Runtime.InteropServices;

namespace SRTPluginProviderRE7.Structs.GameStructs
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 0x8)]

    public struct GamePlayer
    {
        [FieldOffset(0x0)] private float maxHP;
        [FieldOffset(0x4)] private float currentHP;

        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public float Percentage => CurrentHP > 0 ? CurrentHP / MaxHP : 0f;
        public bool IsAlive => CurrentHP != 0 && MaxHP != 0 && CurrentHP > 0 && CurrentHP <= MaxHP;
        public PlayerStatus HealthState
        {
            get =>
                !IsAlive ? PlayerStatus.Dead :
                Percentage >= 0.66f ? PlayerStatus.Fine :
                Percentage >= 0.33f ? PlayerStatus.Caution : 
                PlayerStatus.Danger;
        }
    }

    public enum PlayerStatus
    {
        Dead,
        Fine,
        Caution,
        Danger
    }
}