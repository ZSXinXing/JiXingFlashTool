using System;
using System.Collections.Generic;

namespace JXHeimdall.Models
{
    /// <summary>
    /// 表示一次 Samsung Download 模式刷机请求。
    /// </summary>
    public sealed class HeimdallFlashRequest
    {
        /// <summary>
        /// 目标 Download 模式设备。
        /// </summary>
        public HeimdallDeviceModel Device { get; set; }

        /// <summary>
        /// 固件包路径，键为 BL、AP、CSC、USERDATA 或 TWRP。
        /// </summary>
        public Dictionary<HeimdallFirmwareSlot, string> FirmwareFiles { get; } = new Dictionary<HeimdallFirmwareSlot, string>();

        /// <summary>
        /// 是否按 TWRP 流程刷入并进入 Recovery。
        /// </summary>
        public bool RebootToRecoveryAfterFlash { get; set; }

        /// <summary>
        /// 进度和日志回调。
        /// </summary>
        public Action<string> Log { get; set; }
    }
}
