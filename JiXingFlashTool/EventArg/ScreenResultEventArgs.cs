using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.EventArg
{
    public class CastScreenResultEventArgs : EventArgs
    {
        public DeviceModel Device { get; set; }
        public bool Result { get; set; }
        public SocketPackageModel SocketPackage { get; set; }
        public CastScreenResultEventArgs(DeviceModel device, SocketPackageModel socketPackage)
        {
            Device = device;
            SocketPackage = socketPackage;
            if (socketPackage != null) Result = true;
            else Result = false;
        }
    }
}
