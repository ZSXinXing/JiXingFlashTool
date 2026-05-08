using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Utils;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// 单条指令任务，根据指令类型执行对应设备动作。
    /// </summary>
    public sealed class SingleCommandTask : IDeviceTask<SingleCommandPayload>
    {
        /// <summary>
        /// 任务类型名称。
        /// </summary>
        public string TaskType => "SingleCommandTask";

        /// <summary>
        /// 执行单条指令。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<SingleCommandPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            var commandType = ctx.Payload.Type;

            ctx.Log?.Invoke(new TaskLog($"开始执行指令 {commandType}"));

            switch (commandType)
            {
                case CommandType.RebootSystem:
                    adb.ExecuteRemoteCommand("reboot system", ctx.CancellationToken);
                    break;
                case CommandType.RebootRecovery:
                    adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);
                    break;
                case CommandType.RebootDownload:
                    adb.ExecuteRemoteCommand("reboot download", ctx.CancellationToken);
                    break;
                case CommandType.OpenFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 1 > /sys/class/camera/flash/rear_flash", ctx.CancellationToken);
                    break;
                case CommandType.CloseFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 0 > /sys/class/camera/flash/rear_flash", ctx.CancellationToken);
                    break;
                case CommandType.ExecuteShell:
                    break;
                case CommandType.WipeUserData:
                    await adb.ExecuteRootCommandAsync("twrp wipe dalvik && twrp wipe cache && twrp wipe data", ctx.CancellationToken);
                    break;
                case CommandType.WipeSystem:
                    await adb.ExecuteRootCommandAsync("twrp wipe system", ctx.CancellationToken);
                    break;
                case CommandType.CloseDeveloperMode:
                    ctx.Log?.Invoke(new TaskLog("开始关闭开发者选项"));
                    await adb.ExecuteRootCommandAsync("settings put global development_settings_enabled 0", ctx.CancellationToken);
                    ctx.Log?.Invoke(new TaskLog("关闭开发者选项完成"));
                    break;
                case CommandType.Skip:
                case CommandType.SkipGuide:
                    ctx.Log?.Invoke(new TaskLog("开始跳过向导"));
                    await adb.ExecuteRootCommandAsync("settings put secure user_setup_complete 1 && settings put global device_provisioned 1", ctx.CancellationToken);
                    ctx.Log?.Invoke(new TaskLog("跳过向导完成"));
                    break;
                case CommandType.Format: await Format(ctx);break;
            }

            ctx.Log?.Invoke(new TaskLog($"指令执行完成: {commandType}"));
        }

        private async Task Format(TaskContext<SingleCommandPayload> ctx) {

            var adb = ctx.Device.GetCapability<IAdbCapability>();

            ctx.Log?.Invoke(new TaskLog("获取资料"));
            string ext4 = adb.GetAvailableExt4FormatCommand();
            if (string.IsNullOrEmpty(ext4))
            {
                ctx.Log?.Invoke(new TaskLog("无法找到对应的格式化工具"));
                return;
            }

            string userData = string.Empty;
            RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/data", out userData);
            if (string.IsNullOrEmpty(userData))
            {
                ctx.Log?.Invoke(new TaskLog("无法找到对应的UserData"));
                return;
            }

            await Task.Delay(2000, ctx.CancellationToken);
            ctx.Log?.Invoke(new TaskLog("开始格式化"));
            var formatCommand = $"{ext4} -F {userData}";
            var result = adb.ExecuteRemoteCommand(formatCommand, ctx.CancellationToken);
            if (result.IndexOf("done", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ctx.Log?.Invoke(new TaskLog("格式成功"));
            }
            else
            {
                ctx.Log?.Invoke(new TaskLog("格式化失败"));
            }
        }
    }
}
