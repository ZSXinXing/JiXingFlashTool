using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LanguageCore;
using LanguageCore.Model;

namespace JiXingFlashTool
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
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
        protected override void OnStartup(StartupEventArgs e)
        {
            LocalizationService.Instance.Init(
                typeof(App).Assembly,
                $"{GetType().Namespace}.Resources.Lang",
                "Lang",
                SupportLanguages);

            base.OnStartup(e);
        }
    }
}
