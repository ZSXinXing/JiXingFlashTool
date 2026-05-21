using JiXingFlashTool.Enums;

namespace JiXingFlashTool.Model.Payload
{
    /// <summary>
    /// 更新 Boot / Recovery 任务参数。
    /// </summary>
    /// <param name="Device">目标设备。</param>
    /// <param name="FilePath">本地文件路径。</param>
    /// <param name="UpdateType">更新类型。</param>
    public sealed record UpdateBootRecoveryPayload(DeviceModel Device, string FilePath, TWRPCommandType UpdateType);
}
