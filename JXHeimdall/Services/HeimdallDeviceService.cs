using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责通过定制 Heimdall 枚举当前处于 Samsung Download 模式的 USB 设备。
    /// </summary>
    public sealed class HeimdallDeviceService
    {
        private readonly HeimdallProcessService _processService;

        /// <summary>
        /// 初始化 Download 设备枚举服务。
        /// </summary>
        /// <param name="processService">Heimdall 进程服务。</param>
        public HeimdallDeviceService(HeimdallProcessService processService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        }

        /// <summary>
        /// 获取当前已经进入 Download 模式的 Samsung 设备列表。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>Download 模式设备列表。</returns>
        public async Task<IReadOnlyList<HeimdallDeviceModel>> GetDownloadModeDevicesAsync(CancellationToken cancellationToken)
        {
            var result = await _processService.ExecuteAsync("devices", null, cancellationToken).ConfigureAwait(false);
            return ParseHeimdallDeviceList(result.StandardOutput);
        }

        /// <summary>
        /// 调用 Heimdall 检查当前连接的单台 Download 模式设备是否可通信。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>可通信时返回 true。</returns>
        public async Task<bool> DetectAsync(CancellationToken cancellationToken)
        {
            var result = await _processService.ExecuteAsync("detect", null, cancellationToken).ConfigureAwait(false);
            var combinedOutput = (result.StandardOutput + result.StandardError).ToLowerInvariant();
            return result.IsSuccess || combinedOutput.Contains("device detected");
        }

        /// <summary>
        /// 解析定制 Heimdall devices 命令输出，得到可并发刷入的设备选择器。
        /// </summary>
        /// <param name="output">heimdall devices 的标准输出。</param>
        /// <returns>Download 模式设备列表。</returns>
        private static IReadOnlyList<HeimdallDeviceModel> ParseHeimdallDeviceList(string output)
        {
            var devices = new List<HeimdallDeviceModel>();
            if (string.IsNullOrWhiteSpace(output))
            {
                return devices;
            }

            var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var values = ParseKeyValueLine(line);
                if (!values.TryGetValue("selector", out var selector) ||
                    string.IsNullOrWhiteSpace(selector))
                {
                    continue;
                }

                devices.Add(new HeimdallDeviceModel
                {
                    DisplayIndex = devices.Count + 1,
                    InstanceId = selector,
                    ContainerId = selector,
                    UsbSelector = selector,
                    UsbBusNumber = GetValue(values, "bus"),
                    UsbAddress = GetValue(values, "address"),
                    UsbPortPath = GetValue(values, "port-path"),
                    VendorId = GetValue(values, "vid"),
                    ProductId = GetValue(values, "pid"),
                    DisplayName = "Samsung Download " + selector,
                    ModelName = "Samsung Download",
                    IsHeimdallDetected = true
                });
            }

            return devices;
        }

        /// <summary>
        /// 解析形如 key=value 的 Heimdall 单行设备信息。
        /// </summary>
        /// <param name="line">单行输出文本。</param>
        /// <returns>键值集合。</returns>
        private static Dictionary<string, string> ParseKeyValueLine(string line)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parts = (line ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var separatorIndex = part.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex >= part.Length - 1)
                {
                    continue;
                }

                result[part.Substring(0, separatorIndex)] = part.Substring(separatorIndex + 1);
            }

            return result;
        }

        /// <summary>
        /// 安全读取解析后的键值。
        /// </summary>
        /// <param name="values">键值集合。</param>
        /// <param name="key">键名。</param>
        /// <returns>存在时返回值，不存在时返回空字符串。</returns>
        private static string GetValue(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }
}
