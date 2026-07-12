namespace JiXingFlashTool.Services
{
    /// <summary>
    /// ROM 后端地址提供器，根据 Debug 或 Release 编译配置返回对应服务器地址。
    /// </summary>
    public static class RomApiEndpointProvider
    {
        /// <summary>
        /// Debug 环境 ROM 后端基地址。
        /// </summary>
        public const string DebugBaseUrl = "https://dev-rom-api.jixing-cc.com/client";

        /// <summary>
        /// Release 环境 ROM 后端基地址。
        /// </summary>
        public const string ReleaseBaseUrl = "https://rom-api.jixing-cc.com/client";

        /// <summary>
        /// 获取当前编译配置对应的 ROM 后端基地址。
        /// </summary>
        /// <returns>当前环境基地址。</returns>
        public static string GetBaseUrl()
        {
#if DEBUG
            return DebugBaseUrl;
#else
            return ReleaseBaseUrl;
#endif
        }
    }
}
