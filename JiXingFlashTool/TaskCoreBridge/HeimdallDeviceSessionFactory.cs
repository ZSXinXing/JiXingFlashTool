using JXHeimdall.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskCore.Abstractions;

namespace JiXingFlashTool.TaskCoreBridge
{
    /// <summary>
    /// Heimdall 设备会话工厂，按 Download 设备标识创建 TaskCore 会话。
    /// </summary>
    public sealed class HeimdallDeviceSessionFactory : IDeviceSessionFactory
    {
        private readonly Func<string, HeimdallDeviceModel> _deviceResolver;

        /// <summary>
        /// 使用设备解析器初始化 Heimdall 会话工厂。
        /// </summary>
        /// <param name="deviceResolver">根据 TaskCore 设备标识获取 Download 模式设备模型的委托。</param>
        public HeimdallDeviceSessionFactory(Func<string, HeimdallDeviceModel> deviceResolver)
        {
            _deviceResolver = deviceResolver ?? throw new ArgumentNullException(nameof(deviceResolver));
        }

        /// <summary>
        /// 创建 Heimdall 设备会话。
        /// </summary>
        /// <param name="deviceId">TaskCore 设备标识。</param>
        /// <param name="ct">任务取消令牌。</param>
        /// <returns>设备会话。</returns>
        public ValueTask<IDeviceSession> CreateAsync(string deviceId, CancellationToken ct = default)
        {
            var device = _deviceResolver(deviceId);
            if (device == null)
            {
                throw new InvalidOperationException($"无法找到 Download 设备: {deviceId}");
            }

            return new ValueTask<IDeviceSession>(new HeimdallDeviceSession(deviceId, device));
        }
    }
}
