using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Threading;
using static System.Windows.Forms.LinkLabel;

namespace JiXingFlashTool.ItemViewModel
{
    /// <summary>
    /// 文件同步页面中的单个设备安装进度模型，负责同步一台设备的安装统计与展示状态。
    /// </summary>
    public partial class DeviceInstallItemViewModel : ObservableObject
    {
        private readonly DeviceInstallModel deviceInstall;

        /// <summary>
        /// 当前设备同步任务模型。
        /// </summary>
        public DeviceInstallModel DeviceInstall { get; }

        public DeviceInstallItemViewModel(DeviceInstallModel deviceInstallModel)
        {
            deviceInstall = deviceInstallModel;
            DeviceInstall = deviceInstallModel;
        }

        /// <summary>
        /// 设备 IP 或主机地址。
        /// </summary>
        public string IP
        {
            get
            {
                if (deviceInstall.Device.Serial.IndexOf(":") == -1)
                {
                    return deviceInstall.Device.Serial;
                }
                else
                {
                    return deviceInstall.Device.Serial.Split(':')[0];
                }
            }
        }

        /// <summary>
        /// 总文件数。
        /// </summary>
        public int SumCount
        {
            get => deviceInstall.SumCount;
            set => SetProperty(deviceInstall.SumCount, value, deviceInstall, (u, n) => u.SumCount = n);
        }

        /// <summary>
        /// 成功文件数。
        /// </summary>
        public int SuccessCount
        {
            get => deviceInstall.SuccessCount;
            set => SetProperty(deviceInstall.SuccessCount, value, deviceInstall, (u, n) => u.SuccessCount = n);
        }

        /// <summary>
        /// 失败文件数。
        /// </summary>
        public int FailureCount
        {
            get => deviceInstall.FailureCount;
            set => SetProperty(deviceInstall.FailureCount, value, deviceInstall, (u, n) => u.FailureCount = n);
        }

        /// <summary>
        /// 当前进度。
        /// </summary>
        public int Progress
        {
            get => deviceInstall.Progress;
            set => SetProperty(deviceInstall.Progress, value, deviceInstall, (u, n) => u.Progress = n);
        }
    }
}

