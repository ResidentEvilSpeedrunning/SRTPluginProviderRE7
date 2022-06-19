using System.Diagnostics;
using System.Collections.Generic;

namespace SRTPluginProviderRE7.Structs
{
    public class JackBoss
    {
        public static Dictionary<float, string> JacksEyes = new Dictionary<float, string>()
        {
            { 1600, "Eye" },
            { 1200, "Eye" },
            { 500, "Eye" },
            { 1000, "Eye" },
            { 800, "Eye" },
        };
    }

    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    public struct JackEyeHP
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
                else
                    return "DEAD / DEAD (0%)";
            }
        }
        public bool IsBoss => JackBoss.JacksEyes.ContainsKey(_maximumHP);
        public float MaximumHP { get => _maximumHP; set => _maximumHP = value; }
        internal float _maximumHP;
        public float CurrentHP { get => _currentHP; set => _currentHP = value; }
        internal float _currentHP;
        public bool IsAlive => CurrentHP > 0 && IsBoss;
        public float Percentage => ((IsAlive) ? CurrentHP / MaximumHP : 0f);
    }
}
