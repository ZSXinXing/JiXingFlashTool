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
    /// 更新 Boot / Recovery 的任务。
    /// </summary>
    public sealed class UpdateBootRecoveryTask : IDeviceTask<UpdateBootRecoveryPayload>
    {
        /// <summary>
        /// 任务类型名称。
        /// </summary>
        public string TaskType => "UpdateBootRecoveryTask";

        /// <summary>
        /// 执行更新 Boot / Recovery 流程。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<UpdateBootRecoveryPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            var filePath = ctx.Payload?.FilePath ?? string.Empty;
            var updateType = ctx.Payload.UpdateType;

            if (!FileUtil.IsLocalFileExists(filePath))
            {
                Log(ctx, "TaskLog_FileNotFoundUnableToUpdate");
                return;
            }

            if (updateType == TWRPCommandType.UpdateTWRP)
            {
                await UpdateRecoveryAsync(ctx, adb, filePath);
                return;
            }

            if (updateType == TWRPCommandType.FlashKernel)
            {
                await UpdateBootAsync(ctx, adb, filePath);
                return;
            }

            Log(ctx, "TaskLog_UnsupportedUpdateType");
        }

        /// <summary>
        /// 更新 Recovery 分区。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="adb">ADB 能力接口。</param>
        /// <param name="filePath">本地镜像路径。</param>
        /// <returns>异步任务。</returns>
        private async Task UpdateRecoveryAsync(TaskContext<UpdateBootRecoveryPayload> ctx, IAdbCapability adb, string filePath)
        {
            string localImagePath = filePath;
            string remoteImagePath = "/data/local/tmp/update_recovery.img";
            string recoveryPath;

            if (!RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/recovery", out recoveryPath))
            {
                Log(ctx, "TaskLog_TwrpNodeNotFound");
                return;
            }

            Log(ctx, "TaskLog_TransferFileStart");
            await Task.Delay(2000);
            adb.PushLocalFile(localImagePath, remoteImagePath, new Progress<int>(value => Log(ctx, "TaskLog_TransferProgress", value)), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);

            if (!(adb.CheckRemoteFileExists(remoteImagePath) && adb.IsRemoteFileMd5EqualToLocalFile(remoteImagePath, localImagePath)))
            {
                Log(ctx, "TaskLog_FilePushFailed");
                return;
            }

            Log(ctx, "TaskLog_UpdateTwrpStart");
            string updateResult = adb.ExecuteRemoteCommand($"dd if={remoteImagePath} of={recoveryPath}", ctx.CancellationToken);
            adb.RemoveRemoteFile(remoteImagePath);
            if (updateResult.IndexOf("records in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                updateResult.IndexOf("records out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log(ctx, "TaskLog_UpdateTwrpSuccessReboot");
                await Task.Delay(5000);
                adb.ExecuteRemoteCommand("reboot recovery");
                return;
            }

            Log(ctx, "TaskLog_UpdateFailed");
        }

        /// <summary>
        /// 更新 Boot 分区。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="adb">ADB 能力接口。</param>
        /// <param name="filePath">本地镜像路径。</param>
        /// <returns>异步任务。</returns>
        private async Task UpdateBootAsync(TaskContext<UpdateBootRecoveryPayload> ctx, IAdbCapability adb, string filePath)
        {
            string localImagePath = filePath;
            string remoteImagePath = "/data/local/tmp/update_boot.img";
            string bootPath = string.Empty;

            if (!RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/boot", out bootPath))
            {
                Log(ctx, "TaskLog_KernelNodeNotFound");
                return;
            }

            Log(ctx, "TaskLog_TransferFileStart");
            adb.PushLocalFile(localImagePath, remoteImagePath, new Progress<int>(value => Log(ctx, "TaskLog_TransferProgress", value)), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);
            if (!(adb.CheckRemoteFileExists(remoteImagePath) && adb.IsRemoteFileMd5EqualToLocalFile(remoteImagePath, localImagePath)))
            {
                Log(ctx, "TaskLog_FilePushFailed");
                return;
            }

            Log(ctx, "TaskLog_UpdateKernelStart");
            await Task.Delay(2000);
            string updateResult = adb.ExecuteRemoteCommand($"dd if={remoteImagePath} of={bootPath}", ctx.CancellationToken);
            adb.RemoveRemoteFile(remoteImagePath);
            if (updateResult.IndexOf("records in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                updateResult.IndexOf("records out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log(ctx, "TaskLog_UpdateKernelSuccessManualBoot");
            }
            else
            {
                Log(ctx, "TaskLog_UpdateFailed");
            }
        }

        /// <summary>
        /// 写入可随语言切换刷新的任务日志。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        private static void Log(TaskContext<UpdateBootRecoveryPayload> ctx, string key, params object[] args)
        {
            ctx.Log?.Invoke(TaskLogLocalizationService.CreateLog(key, args));
        }
    }
}
