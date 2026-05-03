using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.EventHandler
{
    public class DeviceDataEventArgs : EventArgs
    {
        public DeviceDataEventArgs(DeviceData device) => Device = device;

        public DeviceData Device { get; private set; }
    }
}
