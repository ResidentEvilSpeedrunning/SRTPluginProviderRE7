using System.Collections.Generic;
using System.Diagnostics;

namespace SRTPluginProviderRE7.Structs
{
    public class Boss
    {
        public static Dictionary<float, string> Bosses = new Dictionary<float, string>()
        {
            { 700, "Boss" },
            { 2000, "Boss" },
            { 2300, "Boss" },
            { 4500, "Boss" },
            { 6000, "Boss" },
            { 10000, "Boss" },
            { 15000, "Boss" },
            { 28000, "Boss" },
            { 50000, "Boss" }
        };
    }

    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    public struct EnemyHP
    {
        /// <summary>
        /// Debugger display message.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string _DebuggerDisplay
        {
            get
            {
                if (IsAlive)
                    return string.Format("Enemy {0} / {1} ({2:P1})", CurrentHP, MaximumHP, Percentage);
                else if (IsTrigger)
                    return string.Format("Trigger");
                else
                    return "DEAD / DEAD (0%)";
            }
        }
        public bool IsBoss => Boss.Bosses.ContainsKey(_maximumHP);

        //public ushort ID { get; set; }
        public float MaximumHP { get => _maximumHP; set => _maximumHP = value; }
        internal float _maximumHP;
        public float CurrentHP { get => _currentHP; set => _currentHP = value; }
        internal float _currentHP;
        public bool IsPlayer => MaximumHP >= 1000f && MaximumHP <= 1400f;
        public bool IsTrigger => MaximumHP < 700f || MaximumHP >= 30000f;
        public bool IsAlive => !IsPlayer && !IsTrigger && MaximumHP > 0 && CurrentHP > 1 && CurrentHP <= MaximumHP && IsBoss;
        public float Percentage => ((IsAlive) ? CurrentHP / MaximumHP : 0f);
    }
}
