using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Capability;
using JiXingFlashTool.Model;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Services;
using JXAdbCore.Enums;
using JXHeimdall.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// Odin 刷机任务，负责接收固件槽位输入并通过 Heimdall 执行普通刷机或 TWRP 刷入 Recovery 流程。
    /// </summary>
    public sealed class OdinFlashTask : IDeviceTask<OdinFlashPayload>
    {
        /// <summary>
        /// TaskCore 中显示的任务类型名称。
        /// </summary>
        public string TaskType => "OdinFlashTask";

        /// <summary>
        /// 执行 Odin 刷机任务。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<OdinFlashPayload> ctx)
        {
            if (ctx?.Payload == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            ValidatePayload(ctx.Payload);
            var heimdall = ctx.Device.GetCapability<IHeimdallCapability>();
            var request = BuildFlashRequest(ctx);
            var result = ctx.Payload.FlashMode == OdinFlashMode.TwrpRebootRecovery
                ? await heimdall.FlashTwrpAndRebootRecoveryAsync(request, ctx.CancellationToken)
                : await heimdall.FlashFirmwareAndRebootAsync(request, ctx.CancellationToken);

            Log(ctx, result.Message);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Message);
            }

            if (ctx.Payload.FlashMode == OdinFlashMode.TwrpInstallSystem)
            {
                await InstallSystemPackageFromTwrpAsync(ctx);
            }
        }

        /// <summary>
        /// 校验 Odin 刷机任务入参，AP 是普通刷机和 TWRP 刷入的必选固件。
        /// </summary>
        /// <param name="payload">刷机任务参数。</param>
        private static void ValidatePayload(OdinFlashPayload payload)
        {
            if (payload.Device == null)
            {
                throw new ArgumentException("缺少 Download 模式设备信息。", nameof(payload));
            }

            if (string.IsNullOrWhiteSpace(payload.ApFilePath) || !File.Exists(payload.ApFilePath))
            {
                throw new FileNotFoundException("AP 固件不存在，无法执行 Odin 刷机。", payload.ApFilePath);
            }

            ValidateOptionalFile(payload.BlFilePath, "BL");
            ValidateOptionalFile(payload.CpFilePath, "CP");
            ValidateOptionalFile(payload.CscFilePath, "CSC");
            ValidateOptionalFile(payload.UserdataFilePath, "USERDATA");

            if (payload.FlashMode == OdinFlashMode.TwrpInstallSystem)
            {
                ValidateSystemInstallPayload(payload);
            }
        }

        /// <summary>
        /// 校验 TWRP 安装系统流程参数，确保已选择 TWRP 镜像和系统包。
        /// </summary>
        /// <param name="payload">刷机任务参数。</param>
        private static void ValidateSystemInstallPayload(OdinFlashPayload payload)
        {
            if (string.IsNullOrWhiteSpace(payload.TwrpFilePath) || !File.Exists(payload.TwrpFilePath))
            {
                throw new FileNotFoundException("TWRP 镜像不存在，无法执行自动安装系统。", payload.TwrpFilePath);
            }

            if (string.IsNullOrWhiteSpace(payload.SystemPackageFilePath) || !File.Exists(payload.SystemPackageFilePath))
            {
                throw new FileNotFoundException("系统包不存在，无法执行 TWRP 侧载刷入。", payload.SystemPackageFilePath);
            }
        }

        /// <summary>
        /// 校验可选固件槽位，已填写路径时必须存在。
        /// </summary>
        /// <param name="filePath">固件路径。</param>
        /// <param name="slotName">槽位名称。</param>
        private static void ValidateOptionalFile(string filePath, string slotName)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && !File.Exists(filePath))
            {
                throw new FileNotFoundException(slotName + " 固件不存在，无法执行 Odin 刷机。", filePath);
            }
        }

        /// <summary>
        /// 根据任务参数构造 Heimdall 刷机请求。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>Heimdall 刷机请求。</returns>
        private static HeimdallFlashRequest BuildFlashRequest(TaskContext<OdinFlashPayload> ctx)
        {
            var payload = ctx.Payload;
            var request = new HeimdallFlashRequest
            {
                Device = payload.Device,
                RebootToRecoveryAfterFlash = payload.FlashMode == OdinFlashMode.TwrpRebootRecovery,
                RecoveryOverrideFilePath = payload.TwrpFilePath,
                Log = message => Log(ctx, message)
            };

            if (payload.FlashMode == OdinFlashMode.TwrpRebootRecovery)
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.TWRP] = payload.ApFilePath;
                return request;
            }

            if (!string.IsNullOrWhiteSpace(payload.BlFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.BL] = payload.BlFilePath;
            }

            request.FirmwareFiles[HeimdallFirmwareSlot.AP] = payload.ApFilePath;

            if (!string.IsNullOrWhiteSpace(payload.CpFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.CP] = payload.CpFilePath;
            }

            if (!string.IsNullOrWhiteSpace(payload.CscFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.CSC] = payload.CscFilePath;
            }

            if (!string.IsNullOrWhiteSpace(payload.UserdataFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.USERDATA] = payload.UserdataFilePath;
            }

            return request;
        }

        /// <summary>
        /// 在 TWRP 启动后执行清理选项、侧载系统包并重启到系统。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>异步任务。</returns>
        private static async Task InstallSystemPackageFromTwrpAsync(TaskContext<OdinFlashPayload> ctx)
        {
            Log(ctx, "正在等待手机从 Download 重启进入 TWRP。");
            var recoveryDevice = await WaitForSingleAdbDeviceAsync(DeviceState.Recovery, TimeSpan.FromMinutes(5), ctx, "TWRP Recovery");
            IAdbCapability adb = new AdbCapability(recoveryDevice);

            await ExecuteSelectedWipeOptionsAsync(ctx, adb);
            await EnterSideloadAsync(ctx, adb);
            await SideloadSystemPackageAsync(ctx, adb);
            adb = await WaitForRecoveryAfterSideloadAsync(ctx);

            Log(ctx, "正在检查系统是否已成功刷入。");
            if (!await WaitForBootableSystemAfterSideloadAsync(ctx, adb))
            {
                Log(ctx, "系统包侧载已完成，但 TWRP 未暴露可读取的系统构建信息，将继续重启到系统。");
            }

            Log(ctx, "已检测到可启动系统，正在重启到系统。");
            RebootToSystemFromTwrp(ctx, adb);
        }

        /// <summary>
        /// 等待唯一一台指定状态的 ADB 设备出现，避免多设备场景下刷错目标。
        /// </summary>
        /// <param name="state">期望设备状态。</param>
        /// <param name="timeout">等待超时时间。</param>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="stageName">日志展示阶段名称。</param>
        /// <returns>匹配到的设备。</returns>
        private static async Task<DeviceModel> WaitForSingleAdbDeviceAsync(DeviceState state, TimeSpan timeout, TaskContext<OdinFlashPayload> ctx, string stageName)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                var devices = AdbService.Instance.Devices()
                    .Where(item => item != null && item.State == state)
                    .ToList();

                if (devices.Count == 1)
                {
                    Log(ctx, "已检测到 " + stageName + " 设备：" + devices[0].Serial);
                    return devices[0];
                }

                if (devices.Count > 1)
                {
                    throw new InvalidOperationException("检测到多台 " + stageName + " 设备，无法安全判断当前 Odin 任务对应哪一台手机，请一次只执行一台组合安装。");
                }

                await Task.Delay(1000, ctx.CancellationToken);
            }

            throw new TimeoutException("等待设备进入 " + stageName + " 超时。");
        }

        /// <summary>
        /// 根据用户选择执行 TWRP 清理动作。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">ADB 能力。</param>
        /// <returns>异步任务。</returns>
        private static async Task ExecuteSelectedWipeOptionsAsync(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            if (ctx.Payload.WipeDataBeforeSystemFlash)
            {
                Log(ctx, "正在执行双清。");
                adb.ExecuteRemoteCommand("twrp wipe dalvik && twrp wipe cache && twrp wipe data", ctx.CancellationToken);
            }

            if (ctx.Payload.WipeSystemBeforeSystemFlash)
            {
                Log(ctx, "正在清除 system 分区。");
                adb.ExecuteRemoteCommand("twrp wipe system", ctx.CancellationToken);
            }

            if (ctx.Payload.FormatDataBeforeSystemFlash)
            {
                await FormatDataAsync(ctx, adb);
            }
        }

        /// <summary>
        /// 在 TWRP 中格式化 data 分区。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">ADB 能力。</param>
        /// <returns>异步任务。</returns>
        private static async Task FormatDataAsync(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            Log(ctx, "正在格式化 data 分区。");
            await Task.Delay(2000, ctx.CancellationToken);
            var result = adb.ExecuteRemoteCommand("twrp format data", ctx.CancellationToken);
            if (!string.IsNullOrWhiteSpace(result) &&
                (result.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new InvalidOperationException("格式化 data 分区失败：" + result);
            }

            Log(ctx, "data 分区格式化完成。");
        }

        /// <summary>
        /// 请求 TWRP 进入 sideload，并等待设备以 sideload 状态重新出现。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">ADB 能力。</param>
        /// <returns>异步任务。</returns>
        private static async Task EnterSideloadAsync(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            Log(ctx, "正在让 TWRP 进入 sideload。");
            adb.ExecuteRemoteCommand("twrp sideload", ctx.CancellationToken);
            await Task.Delay(2000, ctx.CancellationToken);
            await WaitForSingleAdbDeviceAsync(DeviceState.Sideload, TimeSpan.FromMinutes(2), ctx, "TWRP Sideload");
        }

        /// <summary>
        /// 通过 sideload 刷入系统包。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="recoveryAdb">Recovery 阶段 ADB 能力，用于保留设备序列号。</param>
        /// <returns>异步任务。</returns>
        private static async Task SideloadSystemPackageAsync(TaskContext<OdinFlashPayload> ctx, IAdbCapability recoveryAdb)
        {
            Log(ctx, "开始侧载刷入系统包。");
            await Task.Delay(1000, ctx.CancellationToken);
            var result = recoveryAdb.SideloadFile(ctx.Payload.SystemPackageFilePath, new Progress<int>(progress =>
            {
                Log(ctx, "系统包刷入进度 " + progress + "%");
            }), ctx.CancellationToken);

            if (!string.IsNullOrWhiteSpace(result) &&
                (result.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new InvalidOperationException("系统包侧载刷入失败：" + result);
            }

            Log(ctx, "系统包侧载刷入完成。");
        }

        /// <summary>
        /// 等待 sideload 结束后设备重新回到 TWRP Recovery，并重新创建可用的 ADB 能力。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>Recovery 状态下的 ADB 能力。</returns>
        private static async Task<IAdbCapability> WaitForRecoveryAfterSideloadAsync(TaskContext<OdinFlashPayload> ctx)
        {
            Log(ctx, "正在等待 TWRP 从 sideload 返回 Recovery。");
            var recoveryDevice = await WaitForSingleAdbDeviceAsync(DeviceState.Recovery, TimeSpan.FromMinutes(3), ctx, "TWRP Recovery");
            return new AdbCapability(recoveryDevice);
        }

        /// <summary>
        /// 判断侧载后设备中是否存在可尝试启动的系统。
        /// </summary>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        /// <returns>检测到系统关键路径时返回 true。</returns>
        private static async Task<bool> WaitForBootableSystemAfterSideloadAsync(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            for (var retryIndex = 0; retryIndex < 10; retryIndex++)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                if (HasBootableSystemAfterSideload(ctx, adb))
                {
                    return true;
                }

                await Task.Delay(1000, ctx.CancellationToken);
            }

            return false;
        }

        /// <summary>
        /// 判断侧载后设备中是否存在可尝试启动的系统。
        /// </summary>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        /// <returns>检测到系统关键路径时返回 true。</returns>
        private static bool HasBootableSystemAfterSideload(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            if (adb == null)
            {
                return false;
            }

            var hasSystemBuild = HasInstalledSystemBuild(ctx, adb);
            var hasBootPartition = HasBootPartition(ctx, adb);
            var hasSystemPartition = HasSystemPartitionCandidate(ctx, adb);
            return hasSystemBuild || hasBootPartition && hasSystemPartition;
        }

        /// <summary>
        /// 检查 TWRP 中是否能读取到系统构建文件，避免仅凭空目录误判刷入成功。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        /// <returns>检测到系统 build.prop 返回 true。</returns>
        private static bool HasInstalledSystemBuild(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            var output = adb.ExecuteRemoteCommand(
                "twrp mount system >/dev/null 2>&1; " +
                "twrp mount system_root >/dev/null 2>&1; " +
                "mount /system >/dev/null 2>&1; " +
                "mount /system_root >/dev/null 2>&1; " +
                "for file in /system/build.prop /system/system/build.prop /system_root/build.prop /system_root/system/build.prop; do " +
                "if [ -f \"$file\" ] && grep -q \"^ro.build.version.release=\" \"$file\"; then echo \"1:$file\"; exit; fi; " +
                "done; echo 0");

            var normalizedOutput = (output ?? string.Empty).Trim();
            Log(ctx, "系统刷入检测 build.prop 结果：" + normalizedOutput);
            return normalizedOutput.StartsWith("1:", StringComparison.Ordinal);
        }

        /// <summary>
        /// 在 TWRP 中重启到系统，优先使用 TWRP 指令，失败时回退到通用 reboot system。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        private static void RebootToSystemFromTwrp(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            var rebootResult = adb.ExecuteRemoteCommand("twrp reboot system", ctx.CancellationToken);
            if (string.IsNullOrWhiteSpace(rebootResult) ||
                rebootResult.IndexOf("not found", StringComparison.OrdinalIgnoreCase) < 0 &&
                rebootResult.IndexOf("inaccessible", StringComparison.OrdinalIgnoreCase) < 0 &&
                rebootResult.IndexOf("error", StringComparison.OrdinalIgnoreCase) < 0 &&
                rebootResult.IndexOf("failed", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            Log(ctx, "TWRP 重启指令未成功返回，正在尝试通用重启指令：" + rebootResult.Trim());
            adb.ExecuteRemoteCommand("reboot system", ctx.CancellationToken);
        }

        /// <summary>
        /// 检查设备是否仍存在 boot 分区节点，用于配合系统构建文件判断可启动性。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        /// <returns>检测到 boot 分区节点返回 true。</returns>
        private static bool HasBootPartition(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            if (adb.HasBootableSystem())
            {
                return true;
            }

            var output = adb.ExecuteRemoteCommand(
                "for path in /dev/block/bootdevice/by-name/boot " +
                "/dev/block/platform/soc/1d84000.ufshc/by-name/boot " +
                "/dev/block/platform/11120000.ufs/by-name/boot " +
                "/dev/block/platform/15570000.ufs/by-name/boot " +
                "/dev/block/platform/155a0000.ufs/by-name/boot; do " +
                "if [ -e \"$path\" ]; then echo \"1:$path\"; exit; fi; " +
                "done; echo 0");

            var normalizedOutput = (output ?? string.Empty).Trim();
            Log(ctx, "系统刷入检测 boot 分区结果：" + normalizedOutput);
            return normalizedOutput.StartsWith("1:", StringComparison.Ordinal);
        }

        /// <summary>
        /// 检查设备是否存在 system 相关分区节点，兼容动态分区场景中 TWRP 无法直接挂载 build.prop 的情况。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="adb">Recovery 状态下的 ADB 能力。</param>
        /// <returns>检测到 system 相关分区节点返回 true。</returns>
        private static bool HasSystemPartitionCandidate(TaskContext<OdinFlashPayload> ctx, IAdbCapability adb)
        {
            var output = adb.ExecuteRemoteCommand(
                "for path in /dev/block/mapper/system /dev/block/mapper/system_a /dev/block/mapper/system_b " +
                "/dev/block/mapper/system_ext /dev/block/mapper/product /dev/block/mapper/vendor " +
                "/dev/block/by-name/system /dev/block/by-name/system_a /dev/block/by-name/system_b " +
                "/dev/block/bootdevice/by-name/system /dev/block/bootdevice/by-name/system_a /dev/block/bootdevice/by-name/system_b " +
                "/dev/block/bootdevice/by-name/super; do " +
                "if [ -e \"$path\" ]; then echo \"1:$path\"; exit; fi; " +
                "done; echo 0");

            var normalizedOutput = (output ?? string.Empty).Trim();
            Log(ctx, "系统刷入检测 system 分区结果：" + normalizedOutput);
            return normalizedOutput.StartsWith("1:", StringComparison.Ordinal);
        }

        /// <summary>
        /// 写入 Odin 刷机任务日志。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="message">日志文本。</param>
        private static void Log(TaskContext<OdinFlashPayload> ctx, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ctx.Log?.Invoke(new TaskLog(message));
        }
    }
}
