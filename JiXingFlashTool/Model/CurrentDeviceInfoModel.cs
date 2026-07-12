using JXAdbCore.Enums;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// 当前手机列表展示信息模型，承载主页面设备列表需要展示的核心字段。
    /// </summary>
    public sealed class CurrentDeviceInfoModel
    {
        /// <summary>
        /// ADB 设备序列号。
        /// </summary>
        public string Serial { get; set; } = string.Empty;

        /// <summary>
        /// 设备品牌。
        /// </summary>
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// 设备型号。
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// 安卓系统版本；Recovery 状态下为 TWRP 版本。
        /// </summary>
        public string AndroidVersion { get; set; } = string.Empty;

        /// <summary>
        /// 极星系统版本。
        /// </summary>
        public string SystemVersion { get; set; } = string.Empty;

        /// <summary>
        /// 连接方式文本，当前为 USB 或 Ethernet。
        /// </summary>
        public string ConnectionType { get; set; } = string.Empty;

        /// <summary>
        /// 连接方式对应的语言资源键；USB 为固定通用文本时为空。
        /// </summary>
        public string ConnectionTypeResourceKey { get; set; } = string.Empty;

        /// <summary>
        /// 当前设备状态枚举。
        /// </summary>
        public DeviceState State { get; set; } = DeviceState.Unknown;

        /// <summary>
        /// 当前设备状态文本，供非 UI 场景快速展示。
        /// </summary>
        public string StateText { get; set; } = string.Empty;

        /// <summary>
        /// 当前设备状态对应的语言资源键；Download 为固定通用文本时为空。
        /// </summary>
        public string StateResourceKey { get; set; } = string.Empty;

        /// <summary>
        /// 是否通过以太网连接。
        /// </summary>
        public bool IsEthernetConnection { get; set; }
    }
}
