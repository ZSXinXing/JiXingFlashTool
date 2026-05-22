namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示 Odin 固件包中的业务槽位，用于区分 BL、AP、CSC、USERDATA 与 TWRP。
    /// </summary>
    public enum HeimdallFirmwareSlot
    {
        /// <summary>
        /// Bootloader 固件槽位。
        /// </summary>
        BL,

        /// <summary>
        /// Android 主系统固件槽位。
        /// </summary>
        AP,

        /// <summary>
        /// 区域与运营商配置固件槽位。
        /// </summary>
        CSC,

        /// <summary>
        /// 用户数据固件槽位。
        /// </summary>
        USERDATA,

        /// <summary>
        /// TWRP Recovery 刷入槽位。
        /// </summary>
        TWRP
    }
}
