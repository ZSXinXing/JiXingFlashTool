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
                Log(ctx, "TaskLog_FlashFileMissingUnableToSideload");
                return;
            }

            ctx.Payload.Device.IsKeepLink = true;

        Start_Flash_File:
            Log(ctx, "TaskLog_PrepareEnterSideload");
            adb.ExecuteRemoteCommand("twrp sideload", ctx.CancellationToken);
            await Task.Delay(2000, ctx.CancellationToken);

            Log(ctx, "TaskLog_EnteringSideloadKeepPower");

            while (adb.GetDeviceState() != JXAdbCore.Enums.DeviceState.Sideload &&
                   !ctx.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, ctx.CancellationToken);
            }

            Log(ctx, "TaskLog_EnteredSideloadPrepareFlash");
            await Task.Delay(2000, ctx.CancellationToken);

            Log(ctx, "TaskLog_StartFlashFile");
            var result = adb.SideloadFile(filePath, new Progress<int>(progress =>
            {
                Log(ctx, "TaskLog_FlashProgressKeepPower", progress);
            }), ctx.CancellationToken);

            if (!string.IsNullOrWhiteSpace(result) &&
                (result.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 result.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Log(ctx, "TaskLog_FlashFailedRetry");
                await Task.Delay(5000);
                goto Start_Flash_File;
            }

            ctx.Payload.Device.IsKeepLink = false;
            Log(ctx, "TaskLog_FlashSuccess");
        }

        /// <summary>
        /// 写入可随语言切换刷新的任务日志。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        private static void Log(TaskContext<FlashFilePayload> ctx, string key, params object[] args)
        {
            ctx.Log?.Invoke(TaskLogLocalizationService.CreateLog(key, args));
        }
    }
}
