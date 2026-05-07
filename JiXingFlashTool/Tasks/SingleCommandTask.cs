using System.Threading.Tasks;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
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

            ctx.Log?.Invoke(new TaskLog($"开始执行指令: {commandType}"));

            switch (commandType)
            {
                case CommandType.RebootSystem:
                    await adb.ExecuteRemoteCommandAsync("reboot system", ctx.CancellationToken);
                    break;
                case CommandType.RebootRecovery:
                    await adb.ExecuteRemoteCommandAsync("reboot recovery", ctx.CancellationToken);
                    break;
                case CommandType.RebootDownload:
                    await adb.ExecuteRemoteCommandAsync("reboot download", ctx.CancellationToken);
                    break;
                case CommandType.OpenFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 1 > /sys/class/camera/flash/rear_flash", ctx.CancellationToken);
                    break;
                case CommandType.CloseFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 0 > /sys/class/camera/flash/rear_flash", ctx.CancellationToken);
                    break;
                case CommandType.ExecuteShell:
                    break;
                case CommandType.FlashMagisk:
                    await new FlashRootTask().ExecuteAsync(ctx);
                    break;
            }

            ctx.Log?.Invoke(new TaskLog($"指令执行完成: {commandType}"));
        }
    }
}
