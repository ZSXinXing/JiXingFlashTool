using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Services;
using JiXingFlashTool.ViewModels;
using System;
using System.Linq;
using System.Threading;
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
        public Task ExecuteAsync(TaskContext<SingleCommandPayload> ctx)
        {
            var adb = ctx.Device.GetCapability<IAdbCapability>();
            string deviceSerial = ctx.Device.DeviceId;
            const string embeddedResourceName = "Magisk.apk";
            const string remoteFilePath = "/data/local/tmp/Magisk.zip";

        Push_Root_File:
            ctx.Log?.Invoke(new TaskLog("准备传输 ROOT 文件"));
            adb.PushEmbeddedResource(embeddedResourceName, remoteFilePath,progress: new Progress<int>(value =>ctx.Log?.Invoke(new TaskLog($"传输进度 {value}%"))),cancellationToken: ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("检查推送结果"));
            bool fileExists = adb.CheckRemoteFileExists(remoteFilePath);
            bool md5Match = adb.IsRemoteFileMd5EqualToEmbeddedResource(remoteFilePath, embeddedResourceName);

            if ( !(fileExists && md5Match)) {
                ctx.Log?.Invoke(new TaskLog("推送失败,5秒后重试"));
                Task.Delay(5);
                goto Push_Root_File;
            }

        Write_Update_Command:
            string recoveryCommand = $"boot-recovery\r\n--update_package={remoteFilePath}\r\nreboot";
            ctx.Log?.Invoke(new TaskLog("写入更新指令"));
            adb.WriteTextToRemoteFile(recoveryCommand, "/cache/recovery/command", true, cancellationToken: ctx.CancellationToken);
            if (!adb.IsRemoteFileTextEqual("/cache/recovery/command", recoveryCommand, true))
            {
                ctx.Log?.Invoke(new TaskLog("写入更新指令失败,5秒后重试"));
                Task.Delay(5);
                goto Write_Update_Command;
            }



            ctx.Log?.Invoke(new TaskLog("开始刷入"));
            adb.ExecuteRemoteCommandAsync("reboot recovery", ctx.CancellationToken);

            while (true) {

               bool result = adb.IsDeviceOnline();
                Console.WriteLine($"设备状态:{result}");
                Task.Delay(5);
            }
            //ctx.Log?.Invoke(new TaskLog("等待设备重新开机"));
            //可能会出现offline然后device状态，也可能出现一直device状态


            //执行安装面具应用
            ctx.Log?.Invoke(new TaskLog("安装Magisk应用"));
            adb.InstallApkFromEmbeddedResource(embeddedResourceName, progress: new Progress<int>(value => ctx.Log?.Invoke(new TaskLog($"安装进度 {value}%"))), cancellationToken: ctx.CancellationToken);
            Task.Delay(2);

            ctx.Log?.Invoke(new TaskLog("安装Root完毕"));

            return Task.CompletedTask;
        }

        /// <summary>
        /// 写入 recovery 启动命令。
        /// </summary>
        private static void WriteFlashRecoveryCommand(IAdbCapability adb, TaskContext<SingleCommandPayload> ctx)
        {
            const string remoteFilePath = "/data/local/tmp/Magisk.zip";
            string recoveryCommand = $"boot-recovery\r\n--update_package={remoteFilePath}\r\nreboot";

            for (int retryCount = 1; retryCount <= 3; retryCount++)
            {
                ctx.Log?.Invoke(new TaskLog("写入更新指令"));
                adb.WriteTextToRemoteFile(recoveryCommand, "/cache/recovery/command", true, cancellationToken: ctx.CancellationToken);
                if (adb.IsRemoteFileTextEqual("/cache/recovery/command", recoveryCommand, true))
                {
                    return;
                }

                ctx.Log?.Invoke(new TaskLog($"指令写入失败，{retryCount} 次后重试"));
                if (retryCount < 3)
                {
                    Task.Delay(500, ctx.CancellationToken).GetAwaiter().GetResult();
                }
            }
        }

    }
}
