using JiXingFlashTool.Enums;
using JXHeimdall.Models;

namespace JiXingFlashTool.Model.Payload
{
    /// <summary>
    /// Odin 刷机任务参数，保存目标 Download 设备、固件槽位路径和刷机模式。
    /// </summary>
    public sealed record OdinFlashPayload(
        HeimdallDeviceModel Device,
        string BlFilePath,
        string ApFilePath,
        string TwrpFilePath,
        string CpFilePath,
        string CscFilePath,
        string UserdataFilePath,
        OdinFlashMode FlashMode);
}
