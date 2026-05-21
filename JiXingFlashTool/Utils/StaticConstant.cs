using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Utils
{
    public class StaticConstant
    {
        public static string AppDomainPath = AppDomain.CurrentDomain.BaseDirectory;
        public static string PathAdb { get; } = $"{AppDomainPath}\\Library\\ADB\\adb.exe";
        public static string UpdateKernelPath = "/data/local/tmp/boot.img";
        public static string UpdateSystemPath = "/data/local/tmp/update.zip";
        public static string UpdateSystemFileContext = "boot-recovery\r\n--update_package=/data/local/tmp/update.zip\r\n--wipe_data\r\n--wipe_cache\r\n--wipe_media\r\nreboot";
        public static string UpdateSystemCommandPath = "/cache/recovery/command";
        public static string PortApp { get; } = "scrcpy";
        public static string ScrcpyServerPCPath { get; } = $"{AppDomainPath}\\Resources\\scrcpy-server";
        public static string ScrcpyServerPhonePath { get; } = "/sdcard/data/local/tmp/scrcpy-server.jar";
        public static string FFmpegPath = $"{AppDomainPath}\\Resources\\ffmpeg\\";
        public static string PhoneSyncFolder = $"sdcard/JX/";

        public static string TWRPCommandPath = "/cache/recovery/command";

        /// <summary>
        /// 根据是否清除数据生成系统更新命令内容。
        /// </summary>
        /// <param name="wipeData">是否清除数据。</param>
        /// <returns>更新命令内容。</returns>
        public static string BuildUpdateSystemFileContext(bool wipeData)
        {
            if (!wipeData)
            {
                return "boot-recovery\r\n--update_package=/data/local/tmp/update.zip\r\nreboot";
            }

            return UpdateSystemFileContext;
        }
    }
}

