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
    public partial class CastScreenSettingViewModel : ObservableObject
    {
        public static string CSSCastScreenParamterChangeMessageKey = "CSSCastScreenParamterChangeMessageKey";

        /// <summary>
        /// 当前弹窗实例。
        /// </summary>
        private Dialog dialog;

        /// <summary>
        /// 当前弹窗实例。
        /// </summary>
        public Dialog Dialog
        {
            get => dialog;
            set => SetProperty(ref dialog, value);
        }

        /// <summary>
        /// 小屏分辨率列表。
        /// </summary>
        private List<int> miniCastScreenResolutionList;

        /// <summary>
        /// 小屏分辨率列表。
        /// </summary>
        public List<int> MiniCastScreenResolutionList
        {
            get => miniCastScreenResolutionList;
            set => SetProperty(ref miniCastScreenResolutionList, value);
        }

        /// <summary>
        /// 小屏帧率列表。
        /// </summary>
        private List<int> miniCastScreenRateList;

        /// <summary>
        /// 小屏帧率列表。
        /// </summary>
        public List<int> MiniCastScreenRateList
        {
            get => miniCastScreenRateList;
            set => SetProperty(ref miniCastScreenRateList, value);
        }

        /// <summary>
        /// 主控分辨率列表。
        /// </summary>
        private List<int> controlCastScreenResolutionList;

        /// <summary>
        /// 主控分辨率列表。
        /// </summary>
        public List<int> ControlCastScreenResolutionList
        {
            get => controlCastScreenResolutionList;
            set => SetProperty(ref controlCastScreenResolutionList, value);
        }

        /// <summary>
        /// 主控帧率列表。
        /// </summary>
        private List<int> controlCastScreenRateList;

        /// <summary>
        /// 主控帧率列表。
        /// </summary>
        public List<int> ControlCastScreenRateList
        {
            get => controlCastScreenRateList;
            set => SetProperty(ref controlCastScreenRateList, value);
        }

        /// <summary>
        /// 主控帧率。
        /// </summary>
        private int controlCastScreenRate;

        /// <summary>
        /// 主控帧率。
        /// </summary>
        public int ControlCastScreenRate
        {
            get => controlCastScreenRate;
            set => SetProperty(ref controlCastScreenRate, value);
        }

        /// <summary>
        /// 主控分辨率。
        /// </summary>
        private int controlCastScreenResolution;

        /// <summary>
        /// 主控分辨率。
        /// </summary>
        public int ControlCastScreenResolution
        {
            get => controlCastScreenResolution;
            set => SetProperty(ref controlCastScreenResolution, value);
        }

        /// <summary>
        /// 小屏帧率。
        /// </summary>
        private int miniCastScreenRate;

        /// <summary>
        /// 小屏帧率。
        /// </summary>
        public int MiniCastScreenRate
        {
            get => miniCastScreenRate;
            set => SetProperty(ref miniCastScreenRate, value);
        }

        /// <summary>
        /// 小屏分辨率。
        /// </summary>
        private int miniCastScreenResolution;

        /// <summary>
        /// 小屏分辨率。
        /// </summary>
        public int MiniCastScreenResolution
        {
            get => miniCastScreenResolution;
            set => SetProperty(ref miniCastScreenResolution, value);
        }

        /// <summary>
        /// 保存命令。
        /// </summary>
        public RelayCommand SaveCommand => new Lazy<RelayCommand>(() => new RelayCommand(Save)).Value;

        /// <summary>
        /// 保存群控参数。
        /// </summary>
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

            if (
                oldMiniCastScreenRate != MiniCastScreenRate ||
                oldMiniCastScreenResolution != MiniCastScreenResolution ||
                oldControlCastScreenRate != ControlCastScreenRate ||
                oldControlCastScreenResolution != ControlCastScreenResolution)
            {
                WeakReferenceMessenger.Default.Send<ValueChangedMessage<int>, string>(new ValueChangedMessage<int>(0), CSSCastScreenParamterChangeMessageKey);
            }

            this.Dialog.Close();
        }

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

    }
}
