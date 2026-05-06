using System;
using System.Threading;
using System.Threading.Tasks;
using JXAdbCore.Receivers;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Interface;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;

namespace JiXingFlashTool.Capability
{
    /// <summary>
    /// ADB 能力实现，负责把现有 AdbService 包装成 TaskCore 可调用的设备能力。
    /// </summary>
    public sealed class AdbCapability : IAdbCapability
    {
        private readonly DeviceModel _device;
        private SystemRootType? _rootType;

        /// <summary>
        /// 使用指定设备初始化 ADB 能力。
        /// </summary>
        /// <param name="device">当前设备。</param>
        public AdbCapability(DeviceModel device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        /// 执行同步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <returns>指令输出。</returns>
        public string ExecuteRemoteCommand(string command)
        {
            var receiver = new ConsoleOutputReceiver();
            AdbService.Instance.ExecuteShellCommand(_device, command, receiver);
            return receiver.ToString();
        }

        /// <summary>
        /// 执行异步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        public Task ExecuteRemoteCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AdbService.Instance.ExecuteShellCommand(_device, command, null);
            }, cancellationToken);
        }

        /// <summary>
        /// 获取系统属性。
        /// </summary>
        /// <param name="propKey">属性键名。</param>
        /// <returns>属性值。</returns>
        public string GetProp(string propKey)
        {
            return AdbService.Instance.GetProp(_device, propKey);
        }

        /// <summary>
        /// 获取当前设备的 root 类型。
        /// </summary>
        /// <returns>root 类型。</returns>
        public SystemRootType GetRootType()
        {
            if (_rootType.HasValue)
            {
                return _rootType.Value;
            }

            if (_device?.RootType != SystemRootType.None)
            {
                _rootType = _device.RootType;
                return _rootType.Value;
            }

            var rootType = DetectRootType();
            _device.RootType = rootType;
            _rootType = rootType;
            return rootType;
        }

        /// <summary>
        /// 执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">需要 root 权限的指令。</param>
        /// <returns>指令输出。</returns>
        public string ExecuteRootCommand(string command)
        {
            var rootCommand = BuildRootCommand(command);
            if (string.IsNullOrWhiteSpace(rootCommand))
            {
                return string.Empty;
            }

            return ExecuteRemoteCommand(rootCommand);
        }

        /// <summary>
        /// 异步执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">需要 root 权限的指令。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        public Task ExecuteRootCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rootCommand = BuildRootCommand(command);
                if (!string.IsNullOrWhiteSpace(rootCommand))
                {
                    AdbService.Instance.ExecuteShellCommand(_device, rootCommand, null);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// 判断当前设备 root 类型。
        /// </summary>
        /// <returns>root 类型。</returns>
        private SystemRootType DetectRootType()
        {
            try
            {
                var shellRootResult = ExecuteRemoteCommand("id");
                if (shellRootResult.IndexOf("uid=0", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SystemRootType.Shell;
                }

                var suResult = ExecuteRemoteCommand("su -c id");
                if (suResult.IndexOf("uid=0", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return SystemRootType.Kernelsu;
                }
            }
            catch
            {
            }

            return SystemRootType.None;
        }

        /// <summary>
        /// 根据 root 类型构造需要执行的指令。
        /// </summary>
        /// <param name="command">原始指令。</param>
        /// <returns>可执行指令。</returns>
        private string BuildRootCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return string.Empty;
            }

            var rootType = GetRootType();
            switch (rootType)
            {
                case SystemRootType.Shell:
                    return command;
                case SystemRootType.Kernelsu:
                    return $"su -c \"{command.Replace("\"", "\\\"")}\"";
                default:
                    throw new InvalidOperationException("当前设备没有可用的 root 权限。");
            }
        }
    }
}
