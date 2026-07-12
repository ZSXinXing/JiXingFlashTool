using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Services;
using JXAdbCore.Model;

namespace JiXingFlashTool.Model
{
    public enum DeviceStatus {
        Unknown = 0,
        Device,
        Download,
        Recovery,
        Sideload,
        Offline
    }

    public partial class DeviceModel : DeviceData
    {

        /// <summary>
        /// 获取Ro序列号,这个序列号是跟手机的不会变
        /// </summary>
        public string RoSerialNo { get; set; }
        /// <summary>
        /// 零售型号
        /// </summary>
        public string RetailModel { get; set; }
        /// <summary>
        /// 设备品牌，对应 ro.product.brand。
        /// </summary>
        public string Brand { get; set; }
        /// <summary>
        /// AndroidId
        /// </summary>
        public string AndroidId { get; set; }
        /// <summary>
        /// 安卓版本
        /// </summary>
        public string AndroidVersion { get; set; }
        /// <summary>
        /// Polestar版本
        /// </summary>
        public string PolestarVersion { get; set; }
        /// <summary>
        /// TWRP版本
        /// </summary>
        public string TWRPVersion { get; set; }
        /// <summary>
        /// 编译日期
        /// </summary>
        public string BuildDate { get; set; }
        /// <summary>
        /// 屏幕分辨率
        /// </summary>
        public System.Drawing.Size ScreenSize { get; set; }

        public SystemRootType RootType { get; set; }

        private Thread sideloadThread;
        private CancellationTokenSource sideloadCTS;
        public DeviceModel(DeviceData deviceData)
        {

            this.Serial = deviceData.Serial;
            this.State = deviceData.State;
            this.Model = deviceData.Model;
            this.Product = deviceData.Product;
            this.Name = deviceData.Name;
            this.Brand = string.Empty;
            this.Features = deviceData.Features;
            this.Usb = deviceData.Usb;
            this.TransportId = deviceData.TransportId;
            this.RootType = SystemRootType.None;
            this.AndroidId = "";
            this.AndroidVersion = "";
            this.RoSerialNo = "";
            this.ScreenSize = System.Drawing.Size.Empty;
        }

        public string StateChinese {
            get {
                if (State == JXAdbCore.Enums.DeviceState.Offline) return "离线";
                else if (State == JXAdbCore.Enums.DeviceState.BootLoader) return "挖煤";
                else if (State == JXAdbCore.Enums.DeviceState.Online) return "系统";
                else if (State == JXAdbCore.Enums.DeviceState.Recovery) return "恢复";
                else if (State == JXAdbCore.Enums.DeviceState.Authorizing) return "验证中";
                else if (State == JXAdbCore.Enums.DeviceState.NoPermissions) return "无权限";
                else if (State == JXAdbCore.Enums.DeviceState.Sideload) return "侧载";
                else if (State == JXAdbCore.Enums.DeviceState.Unauthorized) return "未验证";
                else if (State == JXAdbCore.Enums.DeviceState.Host) return "网络模式";
                else return "未知状态";
            }
        }

    }

    public partial class DeviceModel
    {
        /// <summary>
        /// 即使已经断开连接但是还是保持连接状态就是不断开链接
        /// </summary>
        public bool IsKeepLink { get; set; }
    }
}
