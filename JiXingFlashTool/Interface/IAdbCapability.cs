using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Model;
using TaskCore.Abstractions;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// ADB 能力接口，供 TaskCore 任务统一访问设备能力。
    /// </summary>
    public interface IAdbCapability : IDeviceCapability
    {
        /// <summary>
        /// 执行同步 ADB 指令。
        /// </summary>
        string ExecuteRemoteCommand(string command);

        /// <summary>
        /// 执行异步 ADB 指令。
        /// </summary>
        string ExecuteRemoteCommand(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统属性。
        /// </summary>
        string GetProp(string propKey);

        /// <summary>
        /// 获取系统编译日期。
        /// </summary>
        /// <returns>格式为 yyyy-MM-dd 的系统编译日期；无法获取时返回空字符串。</returns>
        string GetBuildDate();

        /// <summary>
        /// 批量获取指定系统属性。
        /// </summary>
        /// <param name="propKeys">属性键名集合。</param>
        /// <returns>属性键名与属性值的只读映射。</returns>
        IReadOnlyDictionary<string, string> GetProps(IEnumerable<string> propKeys);

        /// <summary>
        /// 获取当前设备 root 类型。
        /// </summary>
        SystemRootType GetRootType();

        /// <summary>
        /// 执行需要 root 权限的指令。
        /// </summary>
        string ExecuteRootCommand(string command);

        /// <summary>
        /// 检查当前手机是否在线且可以正常执行 ADB 指令。
        /// </summary>
        bool IsDeviceOnline();

        /// <summary>
        /// 获取当前设备所处状态。
        /// </summary>
        /// <returns>设备状态枚举。</returns>
        JXAdbCore.Enums.DeviceState GetDeviceState();

        /// <summary>
        /// 实时获取当前手机所处状态。
        /// </summary>
        /// <returns>当前手机状态枚举。</returns>
        JXAdbCore.Enums.DeviceState GetCurrentDeviceState();

        /// <summary>
        /// 获取当前手机在设备列表中展示的核心信息。
        /// </summary>
        /// <returns>当前手机展示信息模型。</returns>
        CurrentDeviceInfoModel GetCurrentDeviceInfo();

        /// <summary>
        /// 判断当前设备是否已经安装可正常开机的系统。
        /// </summary>
        /// <returns>已经安装且可正常开机返回 true。</returns>
        bool HasBootableSystem();

        /// <summary>
        /// 恢复当前网络 ADB 设备连接；未连接时执行 connect，离线时先 disconnect 再 connect。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>连接成功返回 true。</returns>
        bool RestoreConnection(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步执行需要 root 权限的指令。
        /// </summary>
        Task ExecuteRootCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 推送本地文件到设备。
        /// </summary>
        void PushLocalFile(string filePath, string remotePath, IProgress<int> progress = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// 安装本地 APK 文件。
        /// </summary>
        void InstallApkFromLocalFile(string apkFilePath, bool useRoot = false, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行侧载刷入指令。
        /// </summary>
        /// <param name="filePath">本地刷入文件路径。</param>
        /// <param name="progress">数字进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>指令输出。</returns>
        string SideloadFile(string filePath, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将文本内容写入设备指定路径。
        /// </summary>
        void WriteTextToRemoteFile(string content, string remotePath, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将文本内容写入设备指定路径，并允许指定是否使用 root 权限。
        /// </summary>
        void WriteTextToRemoteFile(string content, string remotePath, bool useRoot, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 推送程序集嵌入资源到设备。
        /// </summary>
        void PushEmbeddedResource(string resourceName, string remotePath, Assembly assembly = null, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 安装嵌入资源中的 APK 文件。
        /// </summary>
        void InstallApkFromEmbeddedResource(string resourceName, Assembly assembly = null, bool useRoot = false, IProgress<int> progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查设备端文件是否存在。
        /// </summary>
        bool CheckRemoteFileExists(string remotePath, bool useRoot = false);

        /// <summary>
        /// 移除设备端指定文件。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        void RemoveRemoteFile(string remotePath, bool useRoot = false);

        /// <summary>
        /// 检查设备端是否存在指定 shell 指令。
        /// </summary>
        /// <returns>返回可用的格式化 ext4 指令名；都不存在则返回空字符串。</returns>
        string GetAvailableExt4FormatCommand();

        /// <summary>
        /// 比较设备文件与本地文件的 MD5 是否一致。
        /// </summary>
        bool IsRemoteFileMd5EqualToLocalFile(string remotePath, string localFilePath, bool useRoot = false);

        /// <summary>
        /// 比较设备文件与本地文件的大小是否一致。
        /// </summary>
        /// <param name="remotePath">设备文件路径。</param>
        /// <param name="localFilePath">本地文件路径。</param>
        /// <param name="useRoot">是否使用 root 权限。</param>
        /// <returns>文件大小一致返回 true。</returns>
        bool IsRemoteFileSizeEqualToLocalFile(string remotePath, string localFilePath, bool useRoot = false);

        /// <summary>
        /// 比较设备文件与嵌入资源的 MD5 是否一致。
        /// </summary>
        bool IsRemoteFileMd5EqualToEmbeddedResource(string remotePath, string resourceName, Assembly assembly = null, bool useRoot = false);

        /// <summary>
        /// 检查设备指定路径中的文本内容是否与期望内容一致。
        /// </summary>
        bool IsRemoteFileTextEqual(string remotePath, string expectedContent, bool useRoot = false);

        /// <summary>
        /// 获取设备中已安装的软件包名。
        /// </summary>
        /// <param name="includeSystemPackages">是否包含系统应用包名。</param>
        /// <returns>已安装的软件包名列表。</returns>
        IReadOnlyList<string> GetInstalledPackageNames(bool includeSystemPackages = false);

        /// <summary>
        /// 根据包名检查应用是否已安装。
        /// </summary>
        /// <param name="packageName">包名。</param>
        /// <returns>已安装返回 true。</returns>
        bool IsAppInstalled(string packageName);
    }
}
