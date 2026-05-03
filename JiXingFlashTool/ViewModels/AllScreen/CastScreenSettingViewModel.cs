using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JiXingFlashTool.Services;
using JiXingFlashTool.Properties;

namespace JiXingFlashTool.ViewModels.AllScreen
{
    public class CastScreenSettingViewModel : ObservableObject
    {
        public static string CSSCastScreenParamterChangeMessageKey = "CSSCastScreenParamterChangeMessageKey";
        public Dialog Dialog { get; set; }

        private List<int> miniCastScreenResolutionList;
        public List<int> MiniCastScreenResolutionList
        {
            get { return miniCastScreenResolutionList; }
            set { SetProperty(ref miniCastScreenResolutionList, value); }
        }

        private List<int> miniCastScreenRateList;
        public List<int> MiniCastScreenRateList
        {
            get { return miniCastScreenRateList; }
            set { SetProperty(ref miniCastScreenRateList, value); }
        }

        private List<int> controlCastScreenResolutionList;
        public List<int> ControlCastScreenResolutionList
        {
            get { return controlCastScreenResolutionList; }
            set { SetProperty(ref controlCastScreenResolutionList, value); }
        }

        private List<int> controlCastScreenRateList;
        public List<int> ControlCastScreenRateList
        {
            get { return controlCastScreenRateList; }
            set { SetProperty(ref controlCastScreenRateList, value); }
        }

        private int controlCastScreenRate;
        public int ControlCastScreenRate
        {
            get { return controlCastScreenRate; }
            set { SetProperty(ref controlCastScreenRate, value); }
        }

        private int controlCastScreenResolution;
        public int ControlCastScreenResolution
        {
            get { return controlCastScreenResolution; }
            set { SetProperty(ref controlCastScreenResolution, value); }
        }

        private int miniCastScreenRate;
        public int MiniCastScreenRate
        {
            get { return miniCastScreenRate; }
            set { SetProperty(ref miniCastScreenRate, value); }
        }

        private int miniCastScreenResolution;
        public int MiniCastScreenResolution
        {
            get { return miniCastScreenResolution; }
            set { SetProperty(ref miniCastScreenResolution, value); }
        }

        public RelayCommand SaveCommand => new Lazy<RelayCommand>(() => new RelayCommand(Save)).Value;

        public void ViewLoad() {

            MiniCastScreenRateList = AppService.Instance.AppConfig.MiniCastScreenRateDefaultList;
            MiniCastScreenResolutionList = AppService.Instance.AppConfig.MiniCastScreenResolutionDefaultList;
            ControlCastScreenRateList = AppService.Instance.AppConfig.ControlCastScreenRateDefaultList;
            ControlCastScreenResolutionList = AppService.Instance.AppConfig.ControlCastScreenResolutionDefaultList;

            MiniCastScreenRate = AppService.Instance.AppConfig.MiniCastScreenRate;
            MiniCastScreenResolution = AppService.Instance.AppConfig.MiniCastScreenResolution;
            ControlCastScreenRate = AppService.Instance.AppConfig.ControlCastScreenRate;
            ControlCastScreenResolution = AppService.Instance.AppConfig.ControlCastScreenResolution;
        }

        private void Save()
        {

            int oldMiniCastScreenRate = AppService.Instance.AppConfig.MiniCastScreenRate;
            int oldMiniCastScreenResolution  = AppService.Instance.AppConfig.MiniCastScreenResolution;
            int oldControlCastScreenRate = AppService.Instance.AppConfig.ControlCastScreenRate = ControlCastScreenRate;
            int oldControlCastScreenResolution = AppService.Instance.AppConfig.ControlCastScreenResolution;

            AppService.Instance.AppConfig.MiniCastScreenRate = MiniCastScreenRate;
            AppService.Instance.AppConfig.MiniCastScreenResolution = MiniCastScreenResolution;
            AppService.Instance.AppConfig.ControlCastScreenRate = ControlCastScreenRate;
            AppService.Instance.AppConfig.ControlCastScreenResolution = ControlCastScreenResolution;

            AppService.Instance.Save();

            //如果发生改变就通知重新修改
            if (
                oldMiniCastScreenRate != MiniCastScreenRate ||
                oldMiniCastScreenResolution != MiniCastScreenResolution ||
                oldControlCastScreenRate != ControlCastScreenRate ||
                oldControlCastScreenResolution != ControlCastScreenResolution) {

                WeakReferenceMessenger.Default.Send<ValueChangedMessage<int>, string>(new ValueChangedMessage<int>(0), CSSCastScreenParamterChangeMessageKey);
            }


            this.Dialog.Close();
        }
    }
}
