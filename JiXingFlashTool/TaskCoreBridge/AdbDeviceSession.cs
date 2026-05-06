using JiXingFlashTool.Capability;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model;
using TaskCore.Sessions;

namespace JiXingFlashTool.TaskCoreBridge
{
    /// <summary>
    /// ADB 设备会话，负责把设备能力注册给 TaskCore。
    /// </summary>
    public sealed class AdbDeviceSession : DeviceSessionBase
    {
        /// <summary>
        /// 当前会话对应的设备唯一标识。
        /// </summary>
        public override string DeviceId { get; }

        /// <summary>
        /// 初始化会话并注册能力。
        /// </summary>
        /// <param name="device">设备模型。</param>
        public AdbDeviceSession(DeviceModel device)
        {
            DeviceId = device?.Serial ?? string.Empty;
            RegisterCapability<IAdbCapability>(new AdbCapability(device));
        }
    }
}
