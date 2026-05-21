using JiXingFlashTool.Enums;
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
                ctx.Log?.Invoke(new TaskLog("文件不存在，无法更新"));
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

            ctx.Log?.Invoke(new TaskLog("不支持该更新类型。"));
        }

        /// <summary>
        /// 更新 Recovery 分区。
        /// </summary>
        private async Task UpdateRecoveryAsync(TaskContext<UpdateBootRecoveryPayload> ctx, IAdbCapability adb, string filePath)
        {
            var deviceModel = ctx.Payload.Device;
            string localImagePath = filePath;
            string remoteImagePath = "/data/local/tmp/update_recovery.img";
            string recoveryPath;

            if (!RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name,"/recovery",out recoveryPath)) {
                ctx.Log?.Invoke(new TaskLog("无法获取TWRP节点信息"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("开始传输文件"));
            await Task.Delay(2000);
            adb.PushLocalFile(localImagePath, remoteImagePath, new Progress<int>(value => ctx.Log?.Invoke(new TaskLog($"传输进度 {value}%"))), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);

            //检查文件与MD5
            if (!(adb.CheckRemoteFileExists(remoteImagePath) && adb.IsRemoteFileMd5EqualToLocalFile(remoteImagePath, localImagePath)))
            {
                ctx.Log?.Invoke(new TaskLog("文件推送失败，请重新尝试。"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("更新TWRP"));
            string updateResult = adb.ExecuteRemoteCommand($"dd if={remoteImagePath} of={recoveryPath}", ctx.CancellationToken);
            adb.RemoveRemoteFile(remoteImagePath);
            if (updateResult.IndexOf("records in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                updateResult.IndexOf("records out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ctx.Log?.Invoke(new TaskLog("更新成功,5秒后重新进入TWRP生效"));
                await Task.Delay(5000);
                adb.ExecuteRemoteCommand("reboot recovery");
                return;
            }
            else {
                ctx.Log?.Invoke(new TaskLog("更新失败"));
            }
        }

        /// <summary>
        /// 更新 Boot 分区。
        /// </summary>
        private async Task UpdateBootAsync(TaskContext<UpdateBootRecoveryPayload> ctx, IAdbCapability adb, string filePath)
        {
            var deviceModel = ctx.Payload.Device;
            string localImagePath = filePath;
            string remoteImagePath = "/data/local/tmp/update_boot.img";
            string bootPath = string.Empty;

            if (!RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/boot", out bootPath))
            {
                ctx.Log?.Invoke(new TaskLog("无法获取内核节点信息"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("传输文件"));
            adb.PushLocalFile(localImagePath, remoteImagePath, new Progress<int>(value => ctx.Log?.Invoke(new TaskLog($"传输进度 {value}%"))), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);
            if ( !(adb.CheckRemoteFileExists(remoteImagePath) && adb.IsRemoteFileMd5EqualToLocalFile(remoteImagePath, localImagePath)))
            {
                ctx.Log?.Invoke(new TaskLog("文件推送失败，请重新尝试。"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("开始更新内核"));
            await Task.Delay(2000);
            string updateResult = adb.ExecuteRemoteCommand($"dd if={remoteImagePath} of={bootPath}", ctx.CancellationToken);
            adb.RemoveRemoteFile(remoteImagePath);
            if (updateResult.IndexOf("records in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                updateResult.IndexOf("records out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ctx.Log?.Invoke(new TaskLog("更新成功,请手动进入系统生效"));
            }
            else {
                ctx.Log?.Invoke(new TaskLog("更新失败"));
            }
        }
    }
}
