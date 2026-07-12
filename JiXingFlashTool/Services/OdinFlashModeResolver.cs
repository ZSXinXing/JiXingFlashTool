using JiXingFlashTool.Enums;
using System;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// Odin 刷机模式解析器，统一根据固件槽位与系统包选择结果判断任务模式。
    /// </summary>
    public static class OdinFlashModeResolver
    {
        /// <summary>
        /// 根据固件选择状态解析 Odin 刷机模式。
        /// </summary>
        /// <param name="hasBlFile">是否已选择 BL 固件。</param>
        /// <param name="hasApFile">是否已选择 AP 固件。</param>
        /// <param name="apFileName">AP 固件文件名。</param>
        /// <param name="hasTwrpFile">是否已选择 TWRP 镜像。</param>
        /// <param name="hasSystemPackageFile">是否已选择 TWRP 侧载系统包。</param>
        /// <param name="hasCpFile">是否已选择 CP 固件。</param>
        /// <param name="hasCscFile">是否已选择 CSC 固件。</param>
        /// <param name="hasUserdataFile">是否已选择 USERDATA 固件。</param>
        /// <returns>当前选择应执行的 Odin 刷机模式。</returns>
        public static OdinFlashMode Resolve(
            bool hasBlFile,
            bool hasApFile,
            string apFileName,
            bool hasTwrpFile,
            bool hasSystemPackageFile,
            bool hasCpFile,
            bool hasCscFile,
            bool hasUserdataFile)
        {
            var isOnlyApSelected = !hasBlFile && hasApFile && !hasTwrpFile && !hasCpFile && !hasCscFile && !hasUserdataFile;
            var normalizedApFileName = apFileName ?? string.Empty;

            if (isOnlyApSelected &&
                (normalizedApFileName.IndexOf("twrp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 normalizedApFileName.EndsWith(".img", StringComparison.OrdinalIgnoreCase)))
            {
                return OdinFlashMode.TwrpRebootRecovery;
            }

            if (hasTwrpFile && hasSystemPackageFile)
            {
                return OdinFlashMode.TwrpInstallSystem;
            }

            return OdinFlashMode.FirmwareReboot;
        }

        /// <summary>
        /// 获取 TaskCore 任务详情文本，方便区分普通刷机、TWRP 刷入和 TWRP 安装系统流程。
        /// </summary>
        /// <param name="flashMode">Odin 刷入模式。</param>
        /// <returns>任务详情文本。</returns>
        public static string GetTaskDetailText(OdinFlashMode flashMode)
        {
            switch (flashMode)
            {
                case OdinFlashMode.TwrpRebootRecovery:
                    return "TWRP-Recovery";
                case OdinFlashMode.TwrpInstallSystem:
                    return "TWRP-Install-System";
                default:
                    return "Firmware-Reboot";
            }
        }
    }
}
