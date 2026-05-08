using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// 刷入文件任务，负责把本地刷机包通过侧载方式刷入设备。
    /// </summary>
    public class FlashFileTask : IDeviceTask<FlashFilePayload>
    {
        /// <summary>
        /// 任务类型标识。
        /// </summary>
        public string TaskType => "FlashFile";

        /// <summary>
        /// 执行文件刷入流程。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<FlashFilePayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            var filePath = ctx.Payload?.filePath ?? string.Empty;

            if (!FileUtil.IsLocalFileExists(filePath))
            {
                ctx.Log?.Invoke(new TaskLog("刷入文件不存在，无法执行侧载。"));
                return;
            }

            ctx.Payload.Device.IsKeepLink = true;

        Start_Flash_File:
            ctx.Log?.Invoke(new TaskLog("准备进入侧载模式"));
            adb.ExecuteRemoteCommand("twrp sideload", ctx.CancellationToken);
            await Task.Delay(2000, ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("正在进入侧载,保持电源,等待重新连接"));

            while (adb.GetDeviceState() != JXAdbCore.Enums.DeviceState.Sideload &&
                   !ctx.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, ctx.CancellationToken);
            }

            ctx.Log?.Invoke(new TaskLog("已经进入侧载,准备刷入文件"));
            await Task.Delay(2000, ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("开始刷入文件"));
            var result = adb.SideloadFile(filePath, new Progress<int>(progress =>
            {
                ctx.Log?.Invoke(new TaskLog($"请保持电源,刷入进度{progress}%"));
            }), ctx.CancellationToken);

            if (!string.IsNullOrWhiteSpace(result) &&
                (result.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                ctx.Log?.Invoke(new TaskLog("刷入失败,5秒后重试"));
                await Task.Delay(5000);
                goto Start_Flash_File;
            }
            ctx.Payload.Device.IsKeepLink = false;
            ctx.Log?.Invoke(new TaskLog("刷入成功"));
        }
    }
}
