using System;
using System.Collections.Generic;

namespace JiXingFlashTool.Utils
{
    /// <summary>
    /// recovery.fstab 预置路径工具类。
    /// </summary>
    public static class RecoveryFstabTool
    {
        private sealed class RecoveryFstabInfo
        {
            public string RecoveryPath { get; }
            public string DataPath { get; }
            public string BootPath { get; }

            public RecoveryFstabInfo(string recoveryPath, string dataPath, string bootPath)
            {
                RecoveryPath = recoveryPath;
                DataPath = dataPath;
                BootPath = bootPath;
            }
        }

        private static readonly Dictionary<string, RecoveryFstabInfo> ModelMap =
            new Dictionary<string, RecoveryFstabInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["dream2lte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/11120000.ufs/by-name/USERDATA",
                    "/dev/block/platform/11120000.ufs/by-name/BOOT"),
                ["dreamlte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/11120000.ufs/by-name/USERDATA",
                    "/dev/block/platform/11120000.ufs/by-name/BOOT"),
                ["gracerlte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/155a0000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/155a0000.ufs/by-name/USERDATA",
                    "/dev/block/platform/155a0000.ufs/by-name/BOOT"),
                ["greatlte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/11120000.ufs/by-name/USERDATA",
                    "/dev/block/platform/11120000.ufs/by-name/BOOT"),
                ["greatqlte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/soc/1da4000.ufshc/by-name/recovery",
                    "/dev/block/platform/soc/1da4000.ufshc/by-name/userdata",
                    "/dev/block/platform/soc/1da4000.ufshc/by-name/boot"),
                ["hero2lte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/155a0000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/155a0000.ufs/by-name/USERDATA",
                    "/dev/block/platform/155a0000.ufs/by-name/BOOT"),
                ["hero2qlte"] = new RecoveryFstabInfo(
                    "/dev/block/bootdevice/by-name/recovery",
                    "/dev/block/bootdevice/by-name/userdata",
                    "/dev/block/bootdevice/by-name/boot"),
                ["herolte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/155a0000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/155a0000.ufs/by-name/USERDATA",
                    "/dev/block/platform/155a0000.ufs/by-name/BOOT"),
                ["heroqlte"] = new RecoveryFstabInfo(
                    "/dev/block/bootdevice/by-name/recovery",
                    "/dev/block/bootdevice/by-name/userdata",
                    "/dev/block/bootdevice/by-name/boot"),
                ["star2lte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/11120000.ufs/by-name/USERDATA",
                    "/dev/block/platform/11120000.ufs/by-name/BOOT"),
                ["star2qltechn"] = new RecoveryFstabInfo(
                    "/dev/block/bootdevice/by-name/recovery",
                    "/dev/block/bootdevice/by-name/userdata",
                    "/dev/block/bootdevice/by-name/boot"),
                ["starlte"] = new RecoveryFstabInfo(
                    "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
                    "/dev/block/platform/11120000.ufs/by-name/USERDATA",
                    "/dev/block/platform/11120000.ufs/by-name/BOOT"),
                ["starqltechn"] = new RecoveryFstabInfo(
                    "/dev/block/platform/soc/1d84000.ufshc/by-name/recovery",
                    "/dev/block/platform/soc/1d84000.ufshc/by-name/userdata",
                    "/dev/block/platform/soc/1d84000.ufshc/by-name/boot")
            };

        /// <summary>
        /// recovery.fstab 路径结果。
        /// </summary>
        public sealed class RecoveryFstabPaths
        {
            /// <summary>
            /// recovery 分区路径。
            /// </summary>
            public string RecoveryPath { get; set; }

            /// <summary>
            /// data 分区路径。
            /// </summary>
            public string DataPath { get; set; }

            /// <summary>
            /// boot 分区路径。
            /// </summary>
            public string BootPath { get; set; }
        }

        /// <summary>
        /// 根据型号获取预置的 recovery、data、boot 路径。
        /// </summary>
        /// <param name="modelName">型号名称，例如 hero2lte。</param>
        /// <param name="paths">输出路径结果。</param>
        /// <returns>存在则返回 true，否则返回 false。</returns>
        public static bool TryGetRecoveryFstabPaths(string modelName, out RecoveryFstabPaths paths)
        {
            paths = null;

            if (string.IsNullOrWhiteSpace(modelName))
            {
                return false;
            }

            if (!ModelMap.TryGetValue(modelName.Trim(), out var info))
            {
                return false;
            }

            paths = new RecoveryFstabPaths
            {
                RecoveryPath = info.RecoveryPath,
                DataPath = info.DataPath,
                BootPath = info.BootPath
            };
            return true;
        }

        /// <summary>
        /// 根据型号和挂载点获取预置的分区路径。
        /// </summary>
        /// <param name="modelName">型号名称，例如 hero2lte。</param>
        /// <param name="mountPoint">挂载点，例如 /recovery、/data、/boot。</param>
        /// <param name="devicePath">输出分区路径。</param>
        /// <returns>存在则返回 true，否则返回 false。</returns>
        public static bool TryGetPartitionPath(string modelName, string mountPoint, out string devicePath)
        {
            devicePath = null;

            if (!TryGetRecoveryFstabPaths(modelName, out var paths))
            {
                return false;
            }

            if (string.Equals(mountPoint, "/recovery", StringComparison.OrdinalIgnoreCase))
            {
                devicePath = paths.RecoveryPath;
            }
            else if (string.Equals(mountPoint, "/data", StringComparison.OrdinalIgnoreCase))
            {
                devicePath = paths.DataPath;
            }
            else if (string.Equals(mountPoint, "/boot", StringComparison.OrdinalIgnoreCase))
            {
                devicePath = paths.BootPath;
            }

            return !string.IsNullOrWhiteSpace(devicePath);
        }
    }
}
