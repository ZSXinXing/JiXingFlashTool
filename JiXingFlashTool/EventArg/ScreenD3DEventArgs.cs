using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.EventArg
{
    public class ScreenD3DEventArgs : EventArgs
    {
        public DeviceModel Device { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ScreenD3DEventArgs(DeviceModel device, int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Device = device;
        }
    }
}
