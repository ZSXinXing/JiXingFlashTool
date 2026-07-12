namespace JiXingFlashTool.Services
{
    /// <summary>
    /// ROM 后端接口路径常量，集中维护接口文档中的相对路径。
    /// </summary>
    public static class RomApiEndpointPaths
    {
        /// <summary>
        /// 检查设备授权接口路径。
        /// </summary>
        public const string CheckDeviceAuthorization = "device/check-auth";

        /// <summary>
        /// 批量提交设备授权结果接口路径。
        /// </summary>
        public const string SubmitDeviceAuthorizationResults = "device/batch-submit-auth-result";

        /// <summary>
        /// 根据代号获取官方系统资源接口路径。
        /// </summary>
        public const string OfficialSystemResourcesByCode = "rom/official-system-resource";

        /// <summary>
        /// 获取公开 ROM 列表接口路径。
        /// </summary>
        public const string PublicRomList = "rom/public-list";

        /// <summary>
        /// 获取 ROM 提取密码接口路径。
        /// </summary>
        public const string RomExtractPassword = "rom/extract-password";

        /// <summary>
        /// 根据代号获取 ROM 资源接口路径。
        /// </summary>
        public const string RomResourcesByCode = "rom/resource";

        /// <summary>
        /// 根据代号获取 TWRP 资源接口路径。
        /// </summary>
        public const string TwrpResourcesByCode = "rom/twrp-resource";

        /// <summary>
        /// 获取客户端高级配置接口路径。
        /// </summary>
        public const string ClientAdvancedConfig = "user/client-advanced-config";

        /// <summary>
        /// 获取客户端公开配置接口路径。
        /// </summary>
        public const string ClientPublicConfig = "user/client-public-config";

        /// <summary>
        /// 客户端登录接口路径。
        /// </summary>
        public const string ClientLogin = "user/client-login";
    }
}
