using JXHeimdall.Models;
using System;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Odin Download 设备串口定位服务，负责从 Windows 设备信息中找到可执行 Odin 协议的 COM 口。
    /// </summary>
    public sealed class OdinSerialPortService
    {
        private static readonly Regex ComPortRegex = new Regex(@"\((COM\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 根据 Download 设备信息解析对应的 Windows 串口名。
        /// </summary>
        /// <param name="device">Download 设备信息。</param>
        /// <returns>可打开的 COM 口名称。</returns>
        public string ResolvePortName(HeimdallDeviceModel device)
        {
            var displayName = device?.DisplayName ?? string.Empty;
            var portName = TryReadPortFromText(displayName);
            if (!string.IsNullOrWhiteSpace(portName))
            {
                return portName;
            }

            var instanceId = device?.InstanceId ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                portName = FindPortByInstanceId(instanceId);
                if (!string.IsNullOrWhiteSpace(portName))
                {
                    return portName;
                }
            }

            portName = FindSamsungDownloadPort();
            if (!string.IsNullOrWhiteSpace(portName))
            {
                return portName;
            }

            throw new InvalidOperationException("未找到 Samsung Download 模式串口，无法执行 Odin 协议刷入。");
        }

        /// <summary>
        /// 从设备显示文本中读取 COM 口名称。
        /// </summary>
        /// <param name="text">设备显示文本。</param>
        /// <returns>COM 口名称，未匹配时返回空字符串。</returns>
        private static string TryReadPortFromText(string text)
        {
            var match = ComPortRegex.Match(text ?? string.Empty);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        /// <summary>
        /// 按设备实例标识查找对应串口。
        /// </summary>
        /// <param name="instanceId">Windows 设备实例标识。</param>
        /// <returns>COM 口名称，未找到时返回空字符串。</returns>
        private static string FindPortByInstanceId(string instanceId)
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_04E8%'"))
            {
                foreach (var queryObject in searcher.Get())
                {
                    using (var deviceObject = (ManagementObject)queryObject)
                    {
                        var currentInstanceId = Convert.ToString(deviceObject.Properties["PNPDeviceID"]?.Value) ?? string.Empty;
                        if (!string.Equals(currentInstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        return TryReadPortFromText(Convert.ToString(deviceObject.Properties["Name"]?.Value));
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 查找当前连接的 Samsung Download 串口。
        /// </summary>
        /// <returns>COM 口名称，未找到时返回空字符串。</returns>
        private static string FindSamsungDownloadPort()
        {
            var availablePorts = SerialPort.GetPortNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE '%VID_04E8%'"))
            {
                foreach (var queryObject in searcher.Get())
                {
                    using (var deviceObject = (ManagementObject)queryObject)
                    {
                        var instanceId = Convert.ToString(deviceObject.Properties["PNPDeviceID"]?.Value) ?? string.Empty;
                        var name = Convert.ToString(deviceObject.Properties["Name"]?.Value) ?? string.Empty;
                        var text = (instanceId + " " + name).ToLowerInvariant();
                        if (!text.Contains("pid_685d") && !text.Contains("download") && !text.Contains("serial"))
                        {
                            continue;
                        }

                        var portName = TryReadPortFromText(name);
                        if (!string.IsNullOrWhiteSpace(portName) && availablePorts.Contains(portName))
                        {
                            return portName;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}
