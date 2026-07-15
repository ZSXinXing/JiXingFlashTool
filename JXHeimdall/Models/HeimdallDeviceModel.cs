namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示当前处于 Samsung Download 模式的 USB 设备信息。
    /// </summary>
    public sealed class HeimdallDeviceModel
    {
        /// <summary>
        /// 设备在列表中的展示序号。
        /// </summary>
        public int DisplayIndex { get; set; }

        /// <summary>
        /// 设备实例标识，多设备刷入时使用 Heimdall USB 选择器。
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备容器标识，多设备刷入时使用 Heimdall USB 选择器。
        /// </summary>
        public string ContainerId { get; set; } = string.Empty;

        /// <summary>
        /// Heimdall 多设备并发刷入时用于锁定具体 USB 设备的选择器。
        /// </summary>
        public string UsbSelector { get; set; } = string.Empty;

        /// <summary>
        /// libusb 识别到的 USB 端口路径。
        /// </summary>
        public string UsbPortPath { get; set; } = string.Empty;

        /// <summary>
        /// libusb 识别到的 USB 总线号。
        /// </summary>
        public string UsbBusNumber { get; set; } = string.Empty;

        /// <summary>
        /// libusb 识别到的 USB 地址。
        /// </summary>
        public string UsbAddress { get; set; } = string.Empty;

        /// <summary>
        /// 设备名称或系统展示名称。
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// USB Vendor Id。
        /// </summary>
        public string VendorId { get; set; } = string.Empty;

        /// <summary>
        /// USB Product Id。
        /// </summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// 当前设备是否通过 Heimdall 探测确认可通信。
        /// </summary>
        public bool IsHeimdallDetected { get; set; }

        /// <summary>
        /// 设备型号展示文本，Download 模式下通常只能显示通用型号。
        /// </summary>
        public string ModelName { get; set; } = "Unknown Samsung";
    }
}
