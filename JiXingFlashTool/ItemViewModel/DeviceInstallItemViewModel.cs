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
    public class DeviceInstallItemViewModel : ObservableObject
    {
        private DeviceInstallModel deviceInstall { get; set; }
        public DeviceInstallItemViewModel(DeviceInstallModel deviceInstallModel)
        {
            this.deviceInstall = deviceInstallModel;
        }


        public DeviceInstallModel DeviceInstall { get { return deviceInstall; } }

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

        public int SumCount
        {
            get
            {
                return deviceInstall.SumCount;
            }
            set
            {
                SetProperty(deviceInstall.SumCount, value, deviceInstall, (u, n) => u.SumCount = n);
            }
        }

        public int SuccessCount
        {
            get
            {
                return deviceInstall.SuccessCount;
            }
            set {
                SetProperty(deviceInstall.SuccessCount, value, deviceInstall, (u, n) => u.SuccessCount = n);
            }
        }

        public int FailureCount
        {
            get
            {
                return deviceInstall.FailureCount;
            }
            set{
                SetProperty(deviceInstall.FailureCount, value, deviceInstall, (u, n) => u.FailureCount = n);
            }

        }

        public int Progress
        {
            get
            {
                return deviceInstall.Progress;
            }
            set {
                SetProperty(deviceInstall.Progress, value, deviceInstall, (u, n) => u.Progress = n);
            }

        }
    }
}

