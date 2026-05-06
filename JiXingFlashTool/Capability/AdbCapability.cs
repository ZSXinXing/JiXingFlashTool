using System.Threading;
using System.Threading.Tasks;
using JXAdbCore.Model;
using JXAdbCore.Receivers;
using JiXingFlashTool.Services;
using JiXingFlashTool.Model;
using JiXingFlashTool.Interface;
namespace JiXingFlashTool.Capability
{
    /// <summary>
    /// ADB 能力实现，内部转发到现有 AdbService。
    /// </summary>
    public sealed class AdbCapability : IAdbCapability
    {
        private readonly DeviceModel _device;

        /// <summary>
        /// 使用指定设备初始化 ADB 能力。
        /// </summary>
        /// <param name="device">当前设备。</param>
        public AdbCapability(DeviceModel device)
        {
            _device = device;
        }

        public string ExecuteRemoteCommand(string command)
        {
            var receiver = new ConsoleOutputReceiver();
            AdbService.Instance.ExecuteShellCommand(_device, command, receiver);
            return receiver.ToString();
        }

        public Task ExecuteRemoteCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                AdbService.Instance.ExecuteShellCommand(_device, command, null);
            }, cancellationToken);
        }

        public string GetProp(string propKey)
        {
            return AdbService.Instance.GetProp(_device, propKey);
        }


        public Task RebootSystem() => ExecuteRemoteCommandAsync("reboot system");
    }
}
