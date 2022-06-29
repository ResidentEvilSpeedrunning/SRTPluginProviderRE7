using System.Diagnostics;
using System.Collections.Generic;

namespace SRTPluginProviderRE7.Structs
{
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
                    return string.Format("Enemy {0} / {1} ({2:P1})", CurrentHP);
                else
                    return "DEAD / DEAD (0%)";
            }
        }
        public float CurrentHP { get => _currentHP; set => _currentHP = value; }
        internal float _currentHP;
        public bool IsAlive => CurrentHP > 0;
    }
}
