using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public struct AdbServerStatus
    {
        /// <summary>
        public bool IsRunning { get; set; }
        public Version Version { get; set; }
        public override string ToString() =>
            IsRunning ? $"Version {Version} of the adb daemon is running." : "The adb daemon is not running.";
    }
}
