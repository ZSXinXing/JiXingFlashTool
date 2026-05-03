using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.EventArg
{
    public class DeviceEventArgs : EventArgs
    { 
        public DeviceModel DeviceModel { get; set; }
        public string RoSerialNo { get; set; }
        public string TestID { get; set; }

        public int Index { get; set; }
        public DeviceEventArgs(DeviceModel deviceModel, string roSerialNo = null)
        {
            DeviceModel = deviceModel;
            RoSerialNo = roSerialNo;
            if (RoSerialNo == null && DeviceModel != null) RoSerialNo = DeviceModel.RoSerialNo;
        }
    }
}
