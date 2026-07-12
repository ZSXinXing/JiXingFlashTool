using JXHeimdall.Services;
using LanguageCore;
using LanguageCore.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TaskCore.Scheduling;
using TaskCore.Sessions;

namespace JiXingFlashTool
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 全局单例获取
        /// </summary>
        public static App Instance => (App)Application.Current;

        /// <summary>
        /// 任务单例管理类
        /// </summary>
        public IDeviceTaskScheduler TaskScheduler { get; private set; }

        /// <summary>
        /// 当前应用支持的界面语言列表。
        /// </summary>
        private static readonly List<LangInfo> SupportLanguages = new List<LangInfo>
        {
            new LangInfo("简体中文", "zh-CN", true),
            new LangInfo("English", "en", false)
        };

        /// <summary>
        /// 应用启动时初始化多语言服务。
        /// </summary>
        /// <param name="e">启动参数。</param>
        protected override async void OnStartup(StartupEventArgs e)
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log"));

            //初始化Heimdall
            HeimdallFirmwarePackageService.CleanupExtractRootDirectory();

            LocalizationService.Instance.Init(
                typeof(App).Assembly,
                $"{GetType().Namespace}.Resources.Lang",
                "Lang",
                SupportLanguages);

            try
            {
                await Services.ResourceFilePathService.Instance.InitializeAsync();
            }
            catch
            {
                // 资源路径服务已记录调试日志，启动时允许资源功能以空配置继续运行。
            }

            //定义手机任务调度类
            var sessionFactory = new TaskCoreBridge.AdbDeviceSessionFactory(deviceId =>
                Services.DeviceService.Instance.DeviceList.FirstOrDefault(device =>
                    device != null && string.Equals(device.Serial, deviceId, StringComparison.OrdinalIgnoreCase)));
            var sessionProvider = new EphemeralDeviceSessionProvider(sessionFactory);
            TaskScheduler = new DeviceTaskScheduler(sessionProvider);

            base.OnStartup(e);
        }

    }
}
