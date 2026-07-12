using JiXingFlashTool.Extensions;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using SharpDX.Direct3D9;
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
            var twrpFilePath = ctx.Payload?.TwrpFilePath ?? string.Empty;


            //由于权限问题临时设置/data/local/tmp权限
            adb.ExecuteRootCommand("restorecon /data/local /data/local/tmp");

            ctx.Log?.Invoke(new TaskLog("开始更新任务"));
            await Task.Delay(2000, ctx.CancellationToken);

            if (!string.IsNullOrWhiteSpace(twrpFilePath))
            {
                if (!FileUtil.IsLocalFileExists(twrpFilePath))
                {
                    ctx.Log?.Invoke(new TaskLog("文件不存在无法更新"));
                    await Task.Delay(2000, ctx.CancellationToken);
                    return;
                }
                else {
                    if (!await TryUpdateTwrpAsync(ctx, adb, twrpFilePath))
                    {
                        ctx.Log?.Invoke(new TaskLog("更新TWRP失败,失败"));
                        await Task.Delay(2000, ctx.CancellationToken);
                        return;
                    }
                }
            }

            //检查刷入的文件是否存在
            if (!FileUtil.IsLocalFileExists(filePath))
            {
                ctx.Log?.Invoke(new TaskLog("文件不存在无法更新"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("开始推送文件"));
            await Task.Delay(2000, ctx.CancellationToken);
            adb.PushLocalFile(filePath, RemoteUpdateFilePath, new Progress<int>(value =>
            {
                ctx.Log?.Invoke(new TaskLog($"推送文件进度:{value}%"));
            }), ctx.CancellationToken);
            await Task.Delay(2000, ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("验证文件"));
            if (!(adb.CheckRemoteFileExists(RemoteUpdateFilePath) && adb.IsRemoteFileSizeEqualToLocalFile(RemoteUpdateFilePath, filePath)))
            {
                ctx.Log?.Invoke(new TaskLog("文件校验失败,请重新尝试"));
                return;
            }
            await Task.Delay(2000, ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("写入更新指令"));
            adb.WriteTextToRemoteFile(BuildRecoveryCommand(ctx.Payload.WipeData), StaticConstant.UpdateSystemCommandPath, true, cancellationToken: ctx.CancellationToken);
            await Task.Delay(500);

            //校验写入的内容
            if (!adb.IsRemoteFileTextEqual(StaticConstant.UpdateSystemCommandPath, BuildRecoveryCommand(ctx.Payload.WipeData), true))
            {
                ctx.Log?.Invoke(new TaskLog("写入更新指令失败,请重新尝试"));
                return;
            }

            ctx.Log?.Invoke(new TaskLog("开始更新"));


            //保持连接状态不断开
            ctx.Payload.Device.IsKeepLink = true;
            adb.ExecuteRemoteCommand("reboot recovery", ctx.CancellationToken);


            //以下可能抓取不到状态-有可能它就不会让adb识别到adb然后直接进入twrp更新之后重新进入系统

            //不断获取手机状态 || 目标刷机包的编译版本号  
            while (adb.GetDeviceState() != JXAdbCore.Enums.DeviceState.Recovery)
            {
                await Task.Delay(500);
                adb.RestoreConnection();
            }

            //更新手机状态
            ctx.Payload.Device.State = JXAdbCore.Enums.DeviceState.Recovery;
            ctx.Payload.NotifyPropertyChanged?.Invoke();

            //这里等待连接以太网连接回来
            ctx.Log?.Invoke(new TaskLog("正在更新,请保持电源"));

            //等待进入系统
            while (adb.GetDeviceState() != JXAdbCore.Enums.DeviceState.Online)
            {
                await Task.Delay(500);
                adb.RestoreConnection();
            }

            ctx.Payload.Device.State = JXAdbCore.Enums.DeviceState.Online;
            //重新获取系统属性,并且更新
            ctx.Payload.Device.PolestarVersion = adb.GetProp("ro.polestar.system.version").ReplaceText("\r\n", "");
            ctx.Payload.Device.AndroidVersion = adb.GetProp("ro.build.version.release").ReplaceText("\r\n", "");
            ctx.Payload.Device.BuildDate = adb.GetBuildDate();
            ctx.Payload.NotifyPropertyChanged?.Invoke();


            ctx.Log?.Invoke(new TaskLog("更新成功"));

            ctx.Payload.Device.IsKeepLink = false;
        }

        /// <summary>
        /// 系统更新前先更新 TWRP Recovery，不立即重启，等待后续系统更新重启时生效。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="adb">ADB 能力接口。</param>
        /// <param name="twrpFilePath">本地 TWRP 镜像路径。</param>
        /// <returns>成功写入 Recovery 分区时返回 true。</returns>
        private static async Task<bool> TryUpdateTwrpAsync(TaskContext<UpdateTaskPayload> ctx, IAdbCapability adb, string twrpFilePath)
        {
            string recoveryPath;
            if (!RecoveryFstabTool.TryGetPartitionPath(ctx.Payload.Device.Name, "/recovery", out recoveryPath))
            {
                return Fail(ctx, "TaskLog_TwrpNodeNotFound");
            }

            ctx.Log?.Invoke(new TaskLog("开始推送TWRP文件"));
            await Task.Delay(2000, ctx.CancellationToken);
            const string remoteImagePath = "/data/local/tmp/update_recovery.img";
            Log(ctx, "TaskLog_TransferFileStart");
            adb.PushLocalFile(twrpFilePath, remoteImagePath, new Progress<int>(value => {
                ctx.Log?.Invoke(new TaskLog($"推送TWRP文件进度:{value}%"));
            }), ctx.CancellationToken);
            await Task.Delay(2000, ctx.CancellationToken);

            ctx.Log?.Invoke(new TaskLog("验证TWRP文件"));
            if (!(adb.CheckRemoteFileExists(remoteImagePath) && adb.IsRemoteFileSizeEqualToLocalFile(remoteImagePath, twrpFilePath)))
            {
                ctx.Log?.Invoke(new TaskLog("验证TWRP文件失败"));
                return false;
            }

            ctx.Log?.Invoke(new TaskLog("执行更新TWRP"));
            string updateResult;
            try
            {
                updateResult = adb.ExecuteRootCommand($"dd if={remoteImagePath} of={recoveryPath}");
            }
            finally
            {
                TryRemoveRemoteFile(adb, remoteImagePath);
            }

            if (updateResult.IndexOf("records in", StringComparison.OrdinalIgnoreCase) < 0 ||
                updateResult.IndexOf("records out", StringComparison.OrdinalIgnoreCase) < 0)
            {
                ctx.Log?.Invoke(new TaskLog("刷入TWRP失败"));
                return false;
            }

            ctx.Log?.Invoke(new TaskLog("更新TWRP成功"));
            return true;
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

        /// <summary>
        /// 写入失败日志并中断任务，避免 TaskCore 把提前返回误显示为任务完成。
        /// </summary>
        /// <param name="ctx">任务上下文。</param>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        private static bool Fail(TaskContext<UpdateTaskPayload> ctx, string key, params object[] args)
        {
            Log(ctx, key, args);
            throw new InvalidOperationException(key);
        }

        /// <summary>
        /// 尝试删除设备端临时文件，避免清理失败覆盖真实刷入结果。
        /// </summary>
        /// <param name="adb">ADB 能力接口。</param>
        /// <param name="remotePath">设备端临时文件路径。</param>
        private static void TryRemoveRemoteFile(IAdbCapability adb, string remotePath)
        {
            try
            {
                adb.RemoveRemoteFile(remotePath);
            }
            catch
            {
            }
        }
    }
}
