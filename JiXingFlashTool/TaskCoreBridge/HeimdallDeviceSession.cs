using JiXingFlashTool.Capability;
using JiXingFlashTool.Interface;
using JXHeimdall.Models;
using TaskCore.Sessions;

namespace JiXingFlashTool.TaskCoreBridge
{
    /// <summary>
    /// Heimdall 设备会话，负责把 Download 模式刷机能力注册给 TaskCore。
    /// </summary>
    public sealed class HeimdallDeviceSession : DeviceSessionBase
    {
        /// <summary>
        /// 当前 Download 设备在 TaskCore 中的唯一标识。
        /// </summary>
        public override string DeviceId { get; }

        /// <summary>
        /// 初始化 Heimdall 设备会话并注册刷机能力。
        /// </summary>
        /// <param name="deviceId">TaskCore 设备标识。</param>
        /// <param name="device">Download 模式设备模型。</param>
        public HeimdallDeviceSession(string deviceId, HeimdallDeviceModel device)
        {
            DeviceId = deviceId ?? string.Empty;
            RegisterCapability<IHeimdallCapability>(new HeimdallCapability());
        }
    }
}
