using JiXingFlashTool.Enums;

namespace JiXingFlashTool.Model.Payload
{
    /// <summary>
    /// 单条任务的参数模型，保存目标设备和对应的指令类型。
    /// </summary>
    public sealed record SingleCommandPayload(DeviceModel Device, CommandType Type);
}
