using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Enums
{
    public enum DeviceState
    {
        Offline = 0,
        BootLoader,
        Online,
        Host,
        Recovery,
        NoPermissions,
        Sideload,
        Unauthorized,
        Authorizing,
        Unknown
    }
}
