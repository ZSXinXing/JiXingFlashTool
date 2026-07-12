using JiXingFlashTool.Capability;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model;
using JiXingFlashTool.Model.Payload;
using JXAdbCore.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// Odin 刷入 TWRP 后的系统安装服务，负责等待 Recovery、执行清理、侧载系统包并重启到系统。
    /// </summary>
    public static class OdinTwrpSystemInstallService
    {
        /// <summary>
        /// 在 TWRP 启动后执行清理选项、侧载系统包并重启到系统。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>异步任务。</returns>
        public static async Task InstallSystemPackageAsync(TaskContext<OdinFlashPayload> ctx)
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

            Log(ctx, "系统包侧载流程已结束，正在重启到系统。");
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
        /// 等待侧载完成后的系统可启动信号，检测失败时由调用流程决定是否继续。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
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
        /// <param name="ctx">TaskCore 任务上下文。</param>
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
        /// 写入 TWRP 系统安装流程日志。
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
