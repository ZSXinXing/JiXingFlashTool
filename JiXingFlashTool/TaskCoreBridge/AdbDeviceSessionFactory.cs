using System;
using System.Threading;
using TaskCore.Abstractions;
using JiXingFlashTool.Model;

namespace JiXingFlashTool.TaskCoreBridge
{
    /// <summary>
    /// ADB 设备会话工厂，按 deviceId 构造 TaskCore 会话。
    /// </summary>
    public sealed class AdbDeviceSessionFactory : IDeviceSessionFactory
    {
        private readonly Func<string, DeviceModel> _deviceResolver;

        /// <summary>
        /// 使用设备解析器初始化工厂。
        /// </summary>
        /// <param name="deviceResolver">根据 deviceId 找到设备模型的委托。</param>
        public AdbDeviceSessionFactory(Func<string, DeviceModel> deviceResolver)
        {
            _deviceResolver = deviceResolver ?? throw new ArgumentNullException(nameof(deviceResolver));
        }

        /// <summary>
        /// 创建设备会话。
        /// </summary>
        /// <param name="deviceId">设备标识。</param>
        /// <param name="ct">取消令牌。</param>
        /// <returns>设备会话。</returns>
        public System.Threading.Tasks.ValueTask<IDeviceSession> CreateAsync(string deviceId, CancellationToken ct = default)
        {
            var device = _deviceResolver(deviceId);
            if (device == null)
            {
                throw new InvalidOperationException($"无法找到设备: {deviceId}");
            }

            return new System.Threading.Tasks.ValueTask<IDeviceSession>(new AdbDeviceSession(device));
        }
    }
}
