using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.Services
{
    public class AppService
    {
        public AppConfigModel AppConfig { get; set; }
        private AppService()
        {
        }
        public static AppService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly AppService instance = new AppService();
        }

        public void Read()
        {
            AppConfig = new AppConfigModel();
            NameValueCollection setting = ConfigurationManager.AppSettings;

            AppConfig.ScreenMiniWidth = int.Parse(setting["ScreenMiniWidth"]);
            AppConfig.ScreenMaxWidth = int.Parse(setting["ScreenMaxWidth"]);
            AppConfig.ScreenCurrentWidth = int.Parse(setting["ScreenCurrentWidth"]);

            AppConfig.MiniCastScreenResolution = int.Parse(setting["MiniCastScreenResolution"]);
            AppConfig.MiniCastScreenRate = int.Parse(setting["MiniCastScreenRate"]);
            AppConfig.ControlCastScreenResolution = int.Parse(setting["ControlCastScreenResolution"]);
            AppConfig.ControlCastScreenRate = int.Parse(setting["ControlCastScreenRate"]);

            if (AppConfig.ScreenMiniWidth == 0 || AppConfig.ScreenMaxWidth == 0 || AppConfig.ScreenCurrentWidth == 0)
            {
                int width = Screen.PrimaryScreen.Bounds.Width;
                AppConfig.ScreenMiniWidth = (width - 300) / 40;
                AppConfig.ScreenMaxWidth = (width - 300) / 5;
                AppConfig.ScreenCurrentWidth = (width - 300) / 10;
            }
        }

        public void Save()
        {

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["ScreenMiniWidth"].Value = AppConfig.ScreenMiniWidth.ToString();
                config.AppSettings.Settings["ScreenMaxWidth"].Value = AppConfig.ScreenMaxWidth.ToString();
                config.AppSettings.Settings["ScreenCurrentWidth"].Value = AppConfig.ScreenCurrentWidth.ToString();

                config.AppSettings.Settings["MiniCastScreenResolution"].Value = AppConfig.MiniCastScreenResolution.ToString();
                config.AppSettings.Settings["MiniCastScreenRate"].Value = AppConfig.MiniCastScreenRate.ToString();
                config.AppSettings.Settings["ControlCastScreenResolution"].Value = AppConfig.ControlCastScreenResolution.ToString();
                config.AppSettings.Settings["ControlCastScreenRate"].Value = AppConfig.ControlCastScreenRate.ToString();
                config.Save(ConfigurationSaveMode.Modified);
            }
            catch
            {

            }
        }
    }
}
