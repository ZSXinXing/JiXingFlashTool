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

        private static readonly RecoveryFstabInfo Exynos8890FstabInfo = new RecoveryFstabInfo(
            "/dev/block/platform/155a0000.ufs/by-name/RECOVERY",
            "/dev/block/platform/155a0000.ufs/by-name/USERDATA",
            "/dev/block/platform/155a0000.ufs/by-name/BOOT");

        private static readonly RecoveryFstabInfo Exynos8895FstabInfo = new RecoveryFstabInfo(
            "/dev/block/platform/11120000.ufs/by-name/RECOVERY",
            "/dev/block/platform/11120000.ufs/by-name/USERDATA",
            "/dev/block/platform/11120000.ufs/by-name/BOOT");

        private static readonly RecoveryFstabInfo QualcommBootDeviceFstabInfo = new RecoveryFstabInfo(
            "/dev/block/bootdevice/by-name/recovery",
            "/dev/block/bootdevice/by-name/userdata",
            "/dev/block/bootdevice/by-name/boot");

        private static readonly RecoveryFstabInfo QualcommGreatFstabInfo = new RecoveryFstabInfo(
            "/dev/block/platform/soc/1da4000.ufshc/by-name/recovery",
            "/dev/block/platform/soc/1da4000.ufshc/by-name/userdata",
            "/dev/block/platform/soc/1da4000.ufshc/by-name/boot");

        private static readonly RecoveryFstabInfo QualcommStarFstabInfo = new RecoveryFstabInfo(
            "/dev/block/platform/soc/1d84000.ufshc/by-name/recovery",
            "/dev/block/platform/soc/1d84000.ufshc/by-name/userdata",
            "/dev/block/platform/soc/1d84000.ufshc/by-name/boot");

        private static readonly Dictionary<string, RecoveryFstabInfo> ModelMap =
            new Dictionary<string, RecoveryFstabInfo>(StringComparer.OrdinalIgnoreCase)
            {
                // Galaxy S7 / S7 edge Exynos variants.
                ["herolte"] = Exynos8890FstabInfo,
                ["heroltexx"] = Exynos8890FstabInfo,
                ["heroltebmc"] = Exynos8890FstabInfo,
                ["heroltektt"] = Exynos8890FstabInfo,
                ["heroltelgt"] = Exynos8890FstabInfo,
                ["herolteskt"] = Exynos8890FstabInfo,
                ["hero2lte"] = Exynos8890FstabInfo,
                ["hero2ltexx"] = Exynos8890FstabInfo,
                ["hero2ltebmc"] = Exynos8890FstabInfo,
                ["hero2ltektt"] = Exynos8890FstabInfo,
                ["hero2ltekt"] = Exynos8890FstabInfo,
                ["hero2ltelgt"] = Exynos8890FstabInfo,
                ["hero2lteskt"] = Exynos8890FstabInfo,

                // Galaxy S7 / S7 edge Qualcomm variants.
                ["heroqlte"] = QualcommBootDeviceFstabInfo,
                ["heroqltechn"] = QualcommBootDeviceFstabInfo,
                ["heroqlteue"] = QualcommBootDeviceFstabInfo,
                ["heroqltesq"] = QualcommBootDeviceFstabInfo,
                ["heroqltespr"] = QualcommBootDeviceFstabInfo,
                ["heroqltevzw"] = QualcommBootDeviceFstabInfo,
                ["heroqlteusc"] = QualcommBootDeviceFstabInfo,
                ["heroqltetmo"] = QualcommBootDeviceFstabInfo,
                ["heroqlteatt"] = QualcommBootDeviceFstabInfo,
                ["heroqltecan"] = QualcommBootDeviceFstabInfo,
                ["hero2qlte"] = QualcommBootDeviceFstabInfo,
                ["hero2qltechn"] = QualcommBootDeviceFstabInfo,
                ["hero2qlteue"] = QualcommBootDeviceFstabInfo,
                ["hero2qltesq"] = QualcommBootDeviceFstabInfo,
                ["hero2qltespr"] = QualcommBootDeviceFstabInfo,
                ["hero2qltevzw"] = QualcommBootDeviceFstabInfo,
                ["hero2qlteusc"] = QualcommBootDeviceFstabInfo,
                ["hero2qltetmo"] = QualcommBootDeviceFstabInfo,
                ["hero2qlteatt"] = QualcommBootDeviceFstabInfo,
                ["hero2qltecan"] = QualcommBootDeviceFstabInfo,

                // Galaxy Note FE Exynos variants.
                ["gracerlte"] = Exynos8890FstabInfo,
                ["gracerltexx"] = Exynos8890FstabInfo,
                ["gracerltektt"] = Exynos8890FstabInfo,
                ["gracerltelgt"] = Exynos8890FstabInfo,
                ["gracerlteskt"] = Exynos8890FstabInfo,

                // Galaxy S8 / S8+ Exynos variants.
                ["dreamlte"] = Exynos8895FstabInfo,
                ["dreamltexx"] = Exynos8895FstabInfo,
                ["dreamltebmc"] = Exynos8895FstabInfo,
                ["dreamltektt"] = Exynos8895FstabInfo,
                ["dreamltelgt"] = Exynos8895FstabInfo,
                ["dreamlteskt"] = Exynos8895FstabInfo,
                ["dream2lte"] = Exynos8895FstabInfo,
                ["dream2ltexx"] = Exynos8895FstabInfo,
                ["dream2ltebmc"] = Exynos8895FstabInfo,
                ["dream2ltektt"] = Exynos8895FstabInfo,
                ["dream2ltelgt"] = Exynos8895FstabInfo,
                ["dream2lteskt"] = Exynos8895FstabInfo,

                // Galaxy S8 / S8+ Qualcomm variants.
                ["dreamqlte"] = QualcommBootDeviceFstabInfo,
                ["dreamqltechn"] = QualcommBootDeviceFstabInfo,
                ["dreamqlteue"] = QualcommBootDeviceFstabInfo,
                ["dreamqltesq"] = QualcommBootDeviceFstabInfo,
                ["dreamqltespr"] = QualcommBootDeviceFstabInfo,
                ["dreamqltevzw"] = QualcommBootDeviceFstabInfo,
                ["dreamqlteusc"] = QualcommBootDeviceFstabInfo,
                ["dreamqltetmo"] = QualcommBootDeviceFstabInfo,
                ["dreamqlteatt"] = QualcommBootDeviceFstabInfo,
                ["dreamqltecan"] = QualcommBootDeviceFstabInfo,
                ["dream2qlte"] = QualcommBootDeviceFstabInfo,
                ["dream2qltechn"] = QualcommBootDeviceFstabInfo,
                ["dream2qlteue"] = QualcommBootDeviceFstabInfo,
                ["dream2qltesq"] = QualcommBootDeviceFstabInfo,
                ["dream2qltespr"] = QualcommBootDeviceFstabInfo,
                ["dream2qltevzw"] = QualcommBootDeviceFstabInfo,
                ["dream2qlteusc"] = QualcommBootDeviceFstabInfo,
                ["dream2qltetmo"] = QualcommBootDeviceFstabInfo,
                ["dream2qlteatt"] = QualcommBootDeviceFstabInfo,
                ["dream2qltecan"] = QualcommBootDeviceFstabInfo,

                // Galaxy Note8 Exynos and Qualcomm variants.
                ["greatlte"] = Exynos8895FstabInfo,
                ["greatltexx"] = Exynos8895FstabInfo,
                ["greatltektt"] = Exynos8895FstabInfo,
                ["greatltelgt"] = Exynos8895FstabInfo,
                ["greatlteskt"] = Exynos8895FstabInfo,
                ["greatqlte"] = QualcommGreatFstabInfo,
                ["greatqltechn"] = QualcommGreatFstabInfo,
                ["greatqlteue"] = QualcommGreatFstabInfo,
                ["greatqltesq"] = QualcommGreatFstabInfo,
                ["greatqltespr"] = QualcommGreatFstabInfo,
                ["greatqltevzw"] = QualcommGreatFstabInfo,
                ["greatqlteusc"] = QualcommGreatFstabInfo,
                ["greatqltetmo"] = QualcommGreatFstabInfo,
                ["greatqlteatt"] = QualcommGreatFstabInfo,
                ["greatqltecan"] = QualcommGreatFstabInfo,

                // Galaxy S9 / S9+ Exynos variants.
                ["starlte"] = Exynos8895FstabInfo,
                ["starltexx"] = Exynos8895FstabInfo,
                ["starltektt"] = Exynos8895FstabInfo,
                ["starltelgt"] = Exynos8895FstabInfo,
                ["starlteskt"] = Exynos8895FstabInfo,
                ["star2lte"] = Exynos8895FstabInfo,
                ["star2ltexx"] = Exynos8895FstabInfo,
                ["star2ltektt"] = Exynos8895FstabInfo,
                ["star2ltelgt"] = Exynos8895FstabInfo,
                ["star2lteskt"] = Exynos8895FstabInfo,

                // Galaxy S9 / S9+ Qualcomm variants.
                ["starqlte"] = QualcommStarFstabInfo,
                ["starqltechn"] = QualcommStarFstabInfo,
                ["starqltecmcc"] = QualcommStarFstabInfo,
                ["starqltecs"] = QualcommStarFstabInfo,
                ["starqltesq"] = QualcommStarFstabInfo,
                ["starqlteue"] = QualcommStarFstabInfo,
                ["starqltespr"] = QualcommStarFstabInfo,
                ["starqltevzw"] = QualcommStarFstabInfo,
                ["starqlteusc"] = QualcommStarFstabInfo,
                ["starqltetmo"] = QualcommStarFstabInfo,
                ["starqlteatt"] = QualcommStarFstabInfo,
                ["starqltecan"] = QualcommStarFstabInfo,
                ["star2qlte"] = QualcommBootDeviceFstabInfo,
                ["star2qltechn"] = QualcommBootDeviceFstabInfo,
                ["star2qltecmcc"] = QualcommBootDeviceFstabInfo,
                ["star2qltecs"] = QualcommBootDeviceFstabInfo,
                ["star2qltesq"] = QualcommBootDeviceFstabInfo,
                ["star2qlteue"] = QualcommBootDeviceFstabInfo,
                ["star2qltespr"] = QualcommBootDeviceFstabInfo,
                ["star2qltevzw"] = QualcommBootDeviceFstabInfo,
                ["star2qlteusc"] = QualcommBootDeviceFstabInfo,
                ["star2qltetmo"] = QualcommBootDeviceFstabInfo,
                ["star2qlteatt"] = QualcommBootDeviceFstabInfo,
                ["star2qltecan"] = QualcommBootDeviceFstabInfo
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

            if (!TryFindFstabInfo(modelName, out var info))
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

        /// <summary>
        /// 根据设备型号查找 fstab 预置项，只使用 ModelMap 中明确维护的设备代号。
        /// </summary>
        /// <param name="modelName">设备型号或代号。</param>
        /// <param name="info">匹配到的 fstab 信息。</param>
        /// <returns>匹配成功时返回 true。</returns>
        private static bool TryFindFstabInfo(string modelName, out RecoveryFstabInfo info)
        {
            info = null;
            return !string.IsNullOrWhiteSpace(modelName) && ModelMap.TryGetValue(modelName.Trim(), out info);
        }
    }
}
