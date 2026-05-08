using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskCore.Tasks;
using JiXingFlashTool.Utils;

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
            string deviceSerial = ctx.Device.DeviceId;
            string localMagiskApkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Magisk.apk");
            const string remoteFilePath = "/data/local/tmp/Magisk.zip";
            const string magiskPackageName = "com.topjohnwu.magisk";
            const string recoveryCommand = $"boot-recovery\r\n--update_package={remoteFilePath}\r\nreboot";
            const string updateCommandPath = "/cache/recovery/command";

            //检查magisk文件是否存在
            if (!FileUtil.IsLocalFileExists(localMagiskApkPath)) {
                throw new FileNotFoundException("Magisk 文件不存在，无法执行 ROOT 刷入。", localMagiskApkPath);
            }

        Push_Root_File:
            ctx.Log?.Invoke(new TaskLog("准备传输 ROOT 文件"));
            adb.PushLocalFile(localMagiskApkPath, remoteFilePath,progress: new Progress<int>(value =>ctx.Log?.Invoke(new TaskLog($"传输进度 {value}%"))),cancellationToken: ctx.CancellationToken);
            await Task.Delay(2000);

            ctx.Log?.Invoke(new TaskLog("检查推送结果"));
            bool fileExists = adb.CheckRemoteFileExists(remoteFilePath);
            bool md5Match = adb.IsRemoteFileMd5EqualToLocalFile(remoteFilePath, localMagiskApkPath);

            if ( !(fileExists && md5Match)) {
                ctx.Log?.Invoke(new TaskLog("推送失败,5秒后重试"));
                await Task.Delay(5000);
                goto Push_Root_File;
            }
            await Task.Delay(2000);

        Write_Update_Command:
            ctx.Log?.Invoke(new TaskLog("写入更新指令"));
            adb.WriteTextToRemoteFile(recoveryCommand, updateCommandPath, true, cancellationToken: ctx.CancellationToken);
            if (!adb.IsRemoteFileTextEqual(updateCommandPath, recoveryCommand, true))
            {
                ctx.Log?.Invoke(new TaskLog("写入更新指令失败,5秒后重试"));
                await Task.Delay(5000);
                goto Write_Update_Command;
            }
            await Task.Delay(2000);

            ctx.Log?.Invoke(new TaskLog("开始刷入"));
            adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);

            //标志离线不需要断开连接
            ctx.Payload.Device.IsKeepLink = true;
            while (adb.IsDeviceOnline()) await Task.Delay(1000);
            ctx.Log?.Invoke(new TaskLog("正在刷入,保持电源,等待重新开机"));

            while (!adb.IsDeviceOnline()) await Task.Delay(1000);

            ctx.Log?.Invoke(new TaskLog("已经成功开机,即将安装Magisk"));
            await Task.Delay(2000);

        Install_Magisk:
            //执行安装面具应用
            ctx.Log?.Invoke(new TaskLog("安装Magisk应用"));
            adb.InstallApkFromLocalFile(localMagiskApkPath, progress: new Progress<int>(value => ctx.Log?.Invoke(new TaskLog($"安装进度 {value}%"))), cancellationToken: ctx.CancellationToken);
            await Task.Delay(2000);

            //检查是否已经安装
            if (!adb.IsAppInstalled(magiskPackageName)) {
                ctx.Log?.Invoke(new TaskLog("安装Magisk失败,5秒之后重试"));
                 await Task.Delay(5000);
                goto Install_Magisk;
            }
            ctx.Log?.Invoke(new TaskLog("安装Root完毕"));
            await Task.Delay(2000);
        }


    }
}
