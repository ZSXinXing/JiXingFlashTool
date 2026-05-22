namespace JiXingFlashTool.Enums
{
    /// <summary>
    /// Odin 刷机任务模式，用于区分普通固件刷入和 TWRP 刷入后进入 Recovery。
    /// </summary>
    public enum OdinFlashMode
    {
        /// <summary>
        /// 刷入 BL、AP、CSC、USERDATA 等 Odin 固件，完成后按 Heimdall 默认流程重启。
        /// </summary>
        FirmwareReboot,

        /// <summary>
        /// 仅刷入 AP 槽位中的 TWRP 包，并执行进入 Recovery 的刷入流程。
        /// </summary>
        TwrpRebootRecovery
    }
}
