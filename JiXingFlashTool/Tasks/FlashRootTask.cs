using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// 刷入 ROOT 的任务，负责把 Magisk 安装包推送到设备并校验结果。
    /// </summary>
    public class FlashRootTask : IDeviceTask<SingleCommandPayload>
    {
        /// <summary>
        /// 任务类型标识。
        /// </summary>
        public string TaskType => "FlashRoot";

        /// <summary>
        /// 执行 ROOT 刷入流程。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<SingleCommandPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            string localMagiskApkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Magisk.apk");
            const string remoteFilePath = "/data/local/tmp/Magisk.zip";
            const string magiskPackageName = "com.topjohnwu.magisk";
            const string recoveryCommand = $"boot-recovery\r\n--update_package={remoteFilePath}\r\nreboot";
            const string updateCommandPath = "/cache/recovery/command";

            // 缺少 Magisk 安装包时先写入本地化日志，再交给任务调度失败流程处理。
            if (!FileUtil.IsLocalFileExists(localMagiskApkPath))
            {
                Log(ctx, "TaskLog_MagiskFileMissingUnableToRoot");
                throw new FileNotFoundException("Magisk file is missing. Unable to flash ROOT.", localMagiskApkPath);
            }

        Push_Root_File:
            Log(ctx, "TaskLog_PrepareTransferRootFile");
            adb.PushLocalFile(
                localMagiskApkPath,
                remoteFilePath,
                progress: new Progress<int>(value => Log(ctx, "TaskLog_TransferProgress", value)),
                cancellationToken: ctx.CancellationToken);
            await Task.Delay(2000);

            Log(ctx, "TaskLog_CheckPushResult");
            bool fileExists = adb.CheckRemoteFileExists(remoteFilePath);
            bool md5Match = adb.IsRemoteFileMd5EqualToLocalFile(remoteFilePath, localMagiskApkPath);

            if (!(fileExists && md5Match))
            {
                Log(ctx, "TaskLog_PushFailedRetry");
                await Task.Delay(5000);
                goto Push_Root_File;
            }
            await Task.Delay(2000);

        Write_Update_Command:
            Log(ctx, "TaskLog_WriteUpdateCommand");
            adb.WriteTextToRemoteFile(recoveryCommand, updateCommandPath, true, cancellationToken: ctx.CancellationToken);
            if (!adb.IsRemoteFileTextEqual(updateCommandPath, recoveryCommand, true))
            {
                Log(ctx, "TaskLog_WriteUpdateCommandFailedRetry");
                await Task.Delay(5000);
                goto Write_Update_Command;
            }
            await Task.Delay(2000);

            Log(ctx, "TaskLog_StartFlashing");
            adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);

            // 标志离线不需要断开连接。
            ctx.Payload.Device.IsKeepLink = true;
            while (adb.IsDeviceOnline()) await Task.Delay(1000);
            Log(ctx, "TaskLog_FlashingWaitRebootKeepPower");

            while (!adb.IsDeviceOnline()) await Task.Delay(1000);

            Log(ctx, "TaskLog_BootedPrepareInstallMagisk");
            await Task.Delay(2000);

        Install_Magisk:
            // 执行安装面具应用。
            Log(ctx, "TaskLog_InstallMagiskApp");
            adb.InstallApkFromLocalFile(
                localMagiskApkPath,
                progress: new Progress<int>(value => Log(ctx, "TaskLog_InstallProgress", value)),
                cancellationToken: ctx.CancellationToken);
            await Task.Delay(2000);

            // 检查是否已经安装。
            if (!adb.IsAppInstalled(magiskPackageName))
            {
                Log(ctx, "TaskLog_InstallMagiskFailedRetry");
                await Task.Delay(5000);
                goto Install_Magisk;
            }

            Log(ctx, "TaskLog_RootInstallComplete");
            await Task.Delay(2000);
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
