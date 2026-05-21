namespace JiXingFlashTool.Model.Payload
{
    /// <summary>
    /// 系统更新任务参数，包含目标设备、本地更新文件和是否清除数据。
    /// </summary>
    /// <param name="Device">目标设备。</param>
    /// <param name="FilePath">本地更新文件路径。</param>
    /// <param name="WipeData">是否在更新前清除用户数据。</param>
    public sealed record UpdateTaskPayload(DeviceModel Device, string FilePath, bool WipeData);
}
