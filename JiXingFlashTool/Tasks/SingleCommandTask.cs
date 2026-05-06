using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using TaskCore.Tasks;
using System.Threading.Tasks;
using JiXingFlashTool.Services;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// 单条通用指令任务，负责根据指令类型执行对应的设备动作。
    /// </summary>
    public sealed class SingleCommandTask : IDeviceTask<SingleCommandPayload>
    {
        /// <summary>
        /// 任务类型名称。
        /// </summary>
        public string TaskType => "SingleCommandTask";

        /// <summary>
        /// 执行单条指令任务。
        /// </summary>
        /// <param name="ctx">任务上下文，包含设备、负载和日志回调。</param>
        /// <returns>异步执行结果。</returns>
        public async Task ExecuteAsync(TaskContext<SingleCommandPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            var commandType = ctx.Payload.Type;

            ctx.Log?.Invoke(new TaskLog($"开始执行指令: {commandType}"));

            switch (commandType)
            {
                case Enums.CommandType.RebootSystem:
                    await adb.ExecuteRemoteCommandAsync("reboot system");
                    break;
                case Enums.CommandType.RebootRecovery:
                    await adb.ExecuteRemoteCommandAsync("reboot recovery");
                    break;
                case Enums.CommandType.RebootDownload:
                    await adb.ExecuteRemoteCommandAsync("reboot download");
                    break;
                case Enums.CommandType.OpenFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 1 > /sys/class/camera/flash/rear_flash");
                    break;
                case Enums.CommandType.CloseFlashlight:
                    await adb.ExecuteRootCommandAsync("echo 0 > /sys/class/camera/flash/rear_flash");
                    break;
                case Enums.CommandType.ExecuteShell:
                    break;
                case Enums.CommandType.FlashMagisk:
                    break;
            }

            ctx.Log?.Invoke(new TaskLog($"指令执行完成: {commandType}"));
        }
    }
}
