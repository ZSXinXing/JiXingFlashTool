using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JiXingFlashTool.Enums;
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
        Task ExecuteRemoteCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统属性。
        /// </summary>
        string GetProp(string propKey);

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
        /// 比较设备文件与本地文件的 MD5 是否一致。
        /// </summary>
        bool IsRemoteFileMd5EqualToLocalFile(string remotePath, string localFilePath, bool useRoot = false);

        /// <summary>
        /// 比较设备文件与嵌入资源的 MD5 是否一致。
        /// </summary>
        bool IsRemoteFileMd5EqualToEmbeddedResource(string remotePath, string resourceName, Assembly assembly = null, bool useRoot = false);

        /// <summary>
        /// 检查设备指定路径中的文本内容是否与期望内容一致。
        /// </summary>
        bool IsRemoteFileTextEqual(string remotePath, string expectedContent, bool useRoot = false);
    }
}
