using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
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

            Log(ctx, "TaskLog_CommandStart", commandType);

            switch (commandType)
            {
                case CommandType.RebootSystem:
                    //需要获取手机所处的状态,处于recovery需要使用twrp的重启指令，否则就使用普通的指令
                    if(adb.GetCurrentDeviceState() == JXAdbCore.Enums.DeviceState.Recovery) adb.ExecuteRemoteCommand("twrp reboot system", ctx.CancellationToken);
                    else adb.ExecuteRemoteCommand("reboot system", ctx.CancellationToken);
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
                    adb.ExecuteRemoteCommand("twrp wipe dalvik && twrp wipe cache && twrp wipe data", ctx.CancellationToken);
                    break;
                case CommandType.WipeSystem:
                    adb.ExecuteRemoteCommand("twrp wipe system", ctx.CancellationToken);
                    break;
                case CommandType.CloseDeveloperMode:
                    Log(ctx, "TaskLog_DisableDeveloperStart");
                    await adb.ExecuteRootCommandAsync("settings put global development_settings_enabled 0", ctx.CancellationToken);
                    Log(ctx, "TaskLog_DisableDeveloperComplete");
                    break;
                case CommandType.Skip:
                case CommandType.SkipGuide:
                    Log(ctx, "TaskLog_SkipGuideStart");
                    await adb.ExecuteRootCommandAsync("settings put secure user_setup_complete 1 && settings put global device_provisioned 1", ctx.CancellationToken);
                    Log(ctx, "TaskLog_SkipGuideComplete");
                    break;
                case CommandType.Format:
                    await FormatAsync(ctx);
                    break;
            }

            Log(ctx, "TaskLog_CommandComplete", commandType);
        }

        /// <summary>
        /// 执行分区格式化。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        private async Task FormatAsync(TaskContext<SingleCommandPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();

            Log(ctx, "TaskLog_GetFormatTool");
            string ext4 = adb.GetAvailableExt4FormatCommand();
            if (string.IsNullOrEmpty(ext4))
            {
                Log(ctx, "TaskLog_FormatToolNotFound");
                return;
            }

            string userData = string.Empty;
            RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/data", out userData);
            if (string.IsNullOrEmpty(userData))
            {
                Log(ctx, "TaskLog_UserDataPartitionNotFound");
                return;
            }

            await Task.Delay(2000, ctx.CancellationToken);
            Log(ctx, "TaskLog_FormatStart");

            string formatCommand = $"{ext4} -F {userData}";
            string result = adb.ExecuteRemoteCommand(formatCommand, ctx.CancellationToken);
            if (result.IndexOf("done", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log(ctx, "TaskLog_FormatSuccess");
            }
            else
            {
                Log(ctx, "TaskLog_FormatFailed");
            }
        }

        /// <summary>
        /// 写入可随语言切换刷新的任务日志。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        private static void Log(TaskContext<SingleCommandPayload> ctx, string key, params object[] args)
        {
            ctx.Log?.Invoke(TaskLogLocalizationService.CreateLog(key, args));
        }
    }
}
