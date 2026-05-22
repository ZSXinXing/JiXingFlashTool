using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责枚举当前 Windows 上处于 Samsung Download 模式的 USB 设备。
    /// </summary>
    public sealed class HeimdallDeviceService
    {
        private static readonly Regex VidRegex = new Regex("VID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PidRegex = new Regex("PID_([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex UsbInterfaceRegex = new Regex(@"^(?<parent>.+?)\\MI_[0-9A-F]{2}\\", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        /// 获取当前已经进入 Download 模式的 Samsung 设备候选列表。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>Download 模式设备列表。</returns>
        public async Task<IReadOnlyList<HeimdallDeviceModel>> GetDownloadModeDevicesAsync(CancellationToken cancellationToken)
        {
            var candidates = new List<HeimdallDeviceModel>();
            var modemDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_04E8%'"))
            {
                foreach (var queryObject in searcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var deviceObject = (ManagementObject)queryObject)
                    {
                        var instanceId = ReadManagementProperty(deviceObject, "PNPDeviceID");
                        var name = ReadManagementProperty(deviceObject, "Name");
                        var service = ReadManagementProperty(deviceObject, "Service");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            name = "Samsung Download Device";
                        }

                        if (LooksLikeModemInterface(instanceId, name, service))
                        {
                            var modemKey = GetUsbProductIdentity(instanceId);
                            if (!string.IsNullOrWhiteSpace(modemKey) && !modemDisplayNames.ContainsKey(modemKey))
                            {
                                modemDisplayNames[modemKey] = name;
                            }
                        }

                        if (!LooksLikeDownloadMode(instanceId, name, service))
                        {
                            continue;
                        }

                        candidates.Add(new HeimdallDeviceModel
                        {
                            InstanceId = instanceId,
                            ContainerId = ReadManagementProperty(deviceObject, "ContainerID"),
                            DisplayName = name,
                            VendorId = ReadRegexValue(VidRegex, instanceId),
                            ProductId = ReadRegexValue(PidRegex, instanceId),
                            ModelName = "Samsung Download"
                        });
                    }
                }
            }

            var devices = DeduplicateDevices(candidates);
            ApplyModemDisplayNames(devices, modemDisplayNames);
            for (var index = 0; index < devices.Count; index++)
            {
                devices[index].DisplayIndex = index + 1;
            }

            if (devices.Count == 1 && _processService.IsAvailable)
            {
                devices[0].IsHeimdallDetected = await DetectAsync(cancellationToken).ConfigureAwait(false);
            }

            return devices;
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
        /// 判断 Windows 设备枚举项是否像 Samsung Download 模式主设备。
        /// </summary>
        /// <param name="instanceId">设备实例标识。</param>
        /// <param name="name">设备显示名称。</param>
        /// <param name="service">设备服务名。</param>
        /// <returns>可能是 Download 主设备时返回 true。</returns>
        private static bool LooksLikeDownloadMode(string instanceId, string name, string service)
        {
            var text = (instanceId + " " + name + " " + service).ToLowerInvariant();
            if (text.Contains("modem"))
            {
                return false;
            }

            return text.Contains("download") ||
                   text.Contains("heimdall") ||
                   text.Contains("gadget serial") ||
                   text.Contains("pid_685d");
        }

        /// <summary>
        /// 判断 Windows 枚举项是否是 Samsung Download 设备暴露出的 Modem 接口。
        /// </summary>
        /// <param name="instanceId">设备实例标识。</param>
        /// <param name="name">设备显示名称。</param>
        /// <param name="service">设备服务名。</param>
        /// <returns>是 Modem 接口时返回 true。</returns>
        private static bool LooksLikeModemInterface(string instanceId, string name, string service)
        {
            var text = (instanceId + " " + name + " " + service).ToLowerInvariant();
            return text.Contains("modem");
        }

        /// <summary>
        /// 将同一台物理手机暴露出的多个 Windows 接口合并为一个设备。
        /// </summary>
        /// <param name="candidates">候选设备集合。</param>
        /// <returns>去重后的设备列表。</returns>
        private static List<HeimdallDeviceModel> DeduplicateDevices(IEnumerable<HeimdallDeviceModel> candidates)
        {
            return candidates
                .GroupBy(GetDeviceIdentity, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(GetDevicePriority).First())
                .OrderBy(device => device.InstanceId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 将同一 Samsung USB 产品下带编号的 Modem 名称用于最终列表显示。
        /// </summary>
        /// <param name="devices">去重后的设备列表。</param>
        /// <param name="modemDisplayNames">按 USB 产品标识归档的 Modem 显示名称。</param>
        private static void ApplyModemDisplayNames(IEnumerable<HeimdallDeviceModel> devices, IReadOnlyDictionary<string, string> modemDisplayNames)
        {
            foreach (var device in devices)
            {
                var displayKey = GetUsbProductIdentity(device.InstanceId);
                if (!string.IsNullOrWhiteSpace(displayKey) && modemDisplayNames.TryGetValue(displayKey, out var modemDisplayName))
                {
                    device.DisplayName = modemDisplayName;
                }
            }
        }

        /// <summary>
        /// 获取用于物理设备去重的稳定标识。
        /// </summary>
        /// <param name="device">候选设备。</param>
        /// <returns>容器标识、USB 父接口标识或实例标识。</returns>
        private static string GetDeviceIdentity(HeimdallDeviceModel device)
        {
            if (!string.IsNullOrWhiteSpace(device.ContainerId))
            {
                return device.ContainerId;
            }

            var instanceId = device.InstanceId ?? string.Empty;
            var parentMatch = UsbInterfaceRegex.Match(instanceId);
            if (parentMatch.Success)
            {
                return parentMatch.Groups["parent"].Value;
            }

            var lastSeparatorIndex = instanceId.LastIndexOf('\\');
            if (lastSeparatorIndex > 0 && lastSeparatorIndex < instanceId.Length - 1)
            {
                return instanceId.Substring(lastSeparatorIndex + 1);
            }

            return instanceId;
        }

        /// <summary>
        /// 获取 USB VID/PID 级别的产品标识，用于把 Modem 接口名称映射回同一台 Samsung Download 设备。
        /// </summary>
        /// <param name="instanceId">Windows 设备实例标识。</param>
        /// <returns>VID/PID 产品标识。</returns>
        private static string GetUsbProductIdentity(string instanceId)
        {
            var vendorId = ReadRegexValue(VidRegex, instanceId);
            var productId = ReadRegexValue(PidRegex, instanceId);
            return string.IsNullOrWhiteSpace(vendorId) || string.IsNullOrWhiteSpace(productId)
                ? string.Empty
                : vendorId + ":" + productId;
        }

        /// <summary>
        /// 计算候选接口优先级，优先保留 Download/Gadget 主接口。
        /// </summary>
        /// <param name="device">候选设备。</param>
        /// <returns>优先级数值。</returns>
        private static int GetDevicePriority(HeimdallDeviceModel device)
        {
            var text = ((device?.InstanceId ?? string.Empty) + " " + (device?.DisplayName ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("download"))
            {
                return 30;
            }

            if (text.Contains("gadget serial") || text.Contains("heimdall"))
            {
                return 20;
            }

            return 10;
        }

        /// <summary>
        /// 从设备实例标识中读取正则匹配值。
        /// </summary>
        /// <param name="regex">正则表达式。</param>
        /// <param name="text">待匹配文本。</param>
        /// <returns>匹配值，失败时返回空字符串。</returns>
        private static string ReadRegexValue(Regex regex, string text)
        {
            var match = regex.Match(text ?? string.Empty);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        /// <summary>
        /// 安全读取 WMI 属性，避免部分 Windows 版本缺少属性时抛出“找不到”异常。
        /// </summary>
        /// <param name="managementObject">WMI 设备对象。</param>
        /// <param name="propertyName">属性名。</param>
        /// <returns>属性文本，缺失时返回空字符串。</returns>
        private static string ReadManagementProperty(ManagementObject managementObject, string propertyName)
        {
            if (managementObject == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return string.Empty;
            }

            try
            {
                var property = managementObject.Properties[propertyName];
                return Convert.ToString(property?.Value) ?? string.Empty;
            }
            catch (ManagementException)
            {
                return string.Empty;
            }
        }
    }
}
