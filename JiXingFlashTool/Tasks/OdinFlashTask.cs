using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model.Payload;
using JXHeimdall.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskCore.Tasks;

namespace JiXingFlashTool.Tasks
{
    /// <summary>
    /// Odin 刷机任务，负责接收固件槽位输入并通过 Heimdall 执行普通刷机或 TWRP 刷入 Recovery 流程。
    /// </summary>
    public sealed class OdinFlashTask : IDeviceTask<OdinFlashPayload>
    {
        /// <summary>
        /// TaskCore 中显示的任务类型名称。
        /// </summary>
        public string TaskType => "OdinFlashTask";

        /// <summary>
        /// 执行 Odin 刷机任务。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>异步任务。</returns>
        public async Task ExecuteAsync(TaskContext<OdinFlashPayload> ctx)
        {
            if (ctx?.Payload == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            ValidatePayload(ctx.Payload);
            var heimdall = ctx.Device.GetCapability<IHeimdallCapability>();
            var request = BuildFlashRequest(ctx);
            var result = ctx.Payload.FlashMode == OdinFlashMode.TwrpRebootRecovery
                ? await heimdall.FlashTwrpAndRebootRecoveryAsync(request, ctx.CancellationToken)
                : await heimdall.FlashFirmwareAndRebootAsync(request, ctx.CancellationToken);

            Log(ctx, result.Message);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Message);
            }
        }

        /// <summary>
        /// 校验 Odin 刷机任务入参，AP 是普通刷机和 TWRP 刷入的必选固件。
        /// </summary>
        /// <param name="payload">刷机任务参数。</param>
        private static void ValidatePayload(OdinFlashPayload payload)
        {
            if (payload.Device == null)
            {
                throw new ArgumentException("缺少 Download 模式设备信息。", nameof(payload));
            }

            if (string.IsNullOrWhiteSpace(payload.ApFilePath) || !File.Exists(payload.ApFilePath))
            {
                throw new FileNotFoundException("AP 固件不存在，无法执行 Odin 刷机。", payload.ApFilePath);
            }

            ValidateOptionalFile(payload.BlFilePath, "BL");
            ValidateOptionalFile(payload.CpFilePath, "CP");
            ValidateOptionalFile(payload.CscFilePath, "CSC");
            ValidateOptionalFile(payload.UserdataFilePath, "USERDATA");
        }

        /// <summary>
        /// 校验可选固件槽位，已填写路径时必须存在。
        /// </summary>
        /// <param name="filePath">固件路径。</param>
        /// <param name="slotName">槽位名称。</param>
        private static void ValidateOptionalFile(string filePath, string slotName)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && !File.Exists(filePath))
            {
                throw new FileNotFoundException(slotName + " 固件不存在，无法执行 Odin 刷机。", filePath);
            }
        }

        /// <summary>
        /// 根据任务参数构造 Heimdall 刷机请求。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <returns>Heimdall 刷机请求。</returns>
        private static HeimdallFlashRequest BuildFlashRequest(TaskContext<OdinFlashPayload> ctx)
        {
            var payload = ctx.Payload;
            var request = new HeimdallFlashRequest
            {
                Device = payload.Device,
                RebootToRecoveryAfterFlash = payload.FlashMode == OdinFlashMode.TwrpRebootRecovery,
                RecoveryOverrideFilePath = payload.TwrpFilePath,
                Log = message => Log(ctx, message)
            };

            if (payload.FlashMode == OdinFlashMode.TwrpRebootRecovery)
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.TWRP] = payload.ApFilePath;
                return request;
            }

            if (!string.IsNullOrWhiteSpace(payload.BlFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.BL] = payload.BlFilePath;
            }

            request.FirmwareFiles[HeimdallFirmwareSlot.AP] = payload.ApFilePath;

            if (!string.IsNullOrWhiteSpace(payload.CpFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.CP] = payload.CpFilePath;
            }

            if (!string.IsNullOrWhiteSpace(payload.CscFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.CSC] = payload.CscFilePath;
            }

            if (!string.IsNullOrWhiteSpace(payload.UserdataFilePath))
            {
                request.FirmwareFiles[HeimdallFirmwareSlot.USERDATA] = payload.UserdataFilePath;
            }

            return request;
        }

        /// <summary>
        /// 写入 Odin 刷机任务日志。
        /// </summary>
        /// <param name="ctx">TaskCore 任务上下文。</param>
        /// <param name="message">日志文本。</param>
        private static void Log(TaskContext<OdinFlashPayload> ctx, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ctx.Log?.Invoke(new TaskLog(message));
        }
    }
}
