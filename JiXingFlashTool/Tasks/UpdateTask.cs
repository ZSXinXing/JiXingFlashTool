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
    /// 系统更新任务，负责把本地更新文件传输到设备并写入 Recovery 更新命令。
    /// </summary>
    public sealed class UpdateTask : IDeviceTask<UpdateTaskPayload>
    {
        private const string RemoteUpdateFilePath = "/data/local/tmp/update";

        /// <summary>
        /// 任务类型名称。
        /// </summary>
        public string TaskType => "UpdateTask";

        /// <summary>
        /// 执行系统更新准备流程。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<UpdateTaskPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            var filePath = ctx.Payload?.FilePath ?? string.Empty;

            if (!FileUtil.IsLocalFileExists(filePath))
            {
                Log(ctx, "TaskLog_FileNotFoundUnableToFlash");
                return;
            }

            Log(ctx, "TaskLog_TransferFileStart");
            adb.PushLocalFile(filePath, RemoteUpdateFilePath, new Progress<int>(value =>
            {
                Log(ctx, "TaskLog_TransferProgress", value);
            }), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);

            if (!(adb.CheckRemoteFileExists(RemoteUpdateFilePath) && adb.IsRemoteFileMd5EqualToLocalFile(RemoteUpdateFilePath, filePath)))
            {
                Log(ctx, "TaskLog_FileTransferVerifyFailed");
                return;
            }

            Log(ctx, "TaskLog_WriteFlashCommand");
            adb.WriteTextToRemoteFile(BuildRecoveryCommand(ctx.Payload.WipeData), StaticConstant.UpdateSystemCommandPath, true, cancellationToken: ctx.CancellationToken);
            await Task.Delay(500);

            if (!adb.IsRemoteFileTextEqual(StaticConstant.UpdateSystemCommandPath, BuildRecoveryCommand(ctx.Payload.WipeData), true))
            {
                Log(ctx, "TaskLog_SystemUpdateCommandVerifyFailed");
                return;
            }

            Log(ctx, "TaskLog_FlashTaskReady");
            ctx.Payload.Device.IsKeepLink = true;
            adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);

            Log(ctx, "TaskLog_FlashingKeepPower");
            while (!adb.IsDeviceOnline()) await Task.Delay(1000);

            Log(ctx, "TaskLog_FileInputSuccess");
            ctx.Payload.Device.IsKeepLink = false;
            await Task.Delay(2000);
        }

        /// <summary>
        /// 根据是否清除数据生成 Recovery 更新命令内容。
        /// </summary>
        /// <param name="wipeData">是否清除用户数据。</param>
        /// <returns>Recovery command 文件内容。</returns>
        private static string BuildRecoveryCommand(bool wipeData)
        {
            if (!wipeData)
            {
                return $"boot-recovery\r\n--update_package={RemoteUpdateFilePath}\r\nreboot";
            }

            return $"boot-recovery\r\n--update_package={RemoteUpdateFilePath}\r\n--wipe_data\r\n--wipe_cache\r\n--wipe_media\r\nreboot";
        }

        /// <summary>
        /// 写入可随语言切换刷新的任务日志。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        private static void Log(TaskContext<UpdateTaskPayload> ctx, string key, params object[] args)
        {
            ctx.Log?.Invoke(TaskLogLocalizationService.CreateLog(key, args));
        }
    }
}
