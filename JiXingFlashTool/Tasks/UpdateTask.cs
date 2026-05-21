using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
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
                ctx.Log?.Invoke(new TaskLog("更新文件不存在，无法执行系统更新。"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("开始传输更新文件。"));
            adb.PushLocalFile(filePath, RemoteUpdateFilePath, new Progress<int>(value =>
            {
                ctx.Log?.Invoke(new TaskLog($"传输进度 {value}%"));
            }), ctx.CancellationToken);

            await Task.Delay(2000, ctx.CancellationToken);

            if (!(adb.CheckRemoteFileExists(RemoteUpdateFilePath) && adb.IsRemoteFileMd5EqualToLocalFile(RemoteUpdateFilePath, filePath)))
            {
                ctx.Log?.Invoke(new TaskLog("更新文件传输校验失败，请重新尝试。"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("写入系统更新命令。"));
            adb.WriteTextToRemoteFile(BuildRecoveryCommand(ctx.Payload.WipeData), StaticConstant.UpdateSystemCommandPath, true, cancellationToken: ctx.CancellationToken);

            if (!adb.IsRemoteFileTextEqual(StaticConstant.UpdateSystemCommandPath, BuildRecoveryCommand(ctx.Payload.WipeData), true))
            {
                ctx.Log?.Invoke(new TaskLog("系统更新命令写入校验失败。"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("系统更新任务准备完成，开始重启到 Recovery。"));
            adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);
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
    }
}
