using System.Diagnostics;

namespace SRTPluginProviderRE7.Structs
{
    [DebuggerDisplay("{_DebuggerDisplay,nq}")]
    public class JackEyeHP
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
                    return string.Format("HP: {0}", CurrentHP);
                else
                    return "DEAD";
            }
        }

        public float CurrentHP { get; set; }
        public bool IsAlive => CurrentHP > 0;

        public JackEyeHP()
        {
            this.CurrentHP = 0;
        }
    }
}
