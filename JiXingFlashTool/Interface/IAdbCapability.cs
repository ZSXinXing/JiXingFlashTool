using System.Threading;
using System.Threading.Tasks;
using JiXingFlashTool.Enums;
using TaskCore.Abstractions;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// ADB 能力接口，供 TaskCore 任务通过会话获取并执行设备指令。
    /// </summary>
    public interface IAdbCapability : IDeviceCapability
    {
        /// <summary>
        /// 执行同步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <returns>指令输出。</returns>
        string ExecuteRemoteCommand(string command);

        /// <summary>
        /// 执行异步 ADB 指令。
        /// </summary>
        /// <param name="command">指令内容。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        Task ExecuteRemoteCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统属性。
        /// </summary>
        /// <param name="propKey">属性键名。</param>
        /// <returns>属性值。</returns>
        string GetProp(string propKey);

        /// <summary>
        /// 获取当前设备的 root 类型。
        /// </summary>
        /// <returns>root 类型。</returns>
        SystemRootType GetRootType();

        /// <summary>
        /// 执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">需要 root 权限的指令。</param>
        /// <returns>指令输出。</returns>
        string ExecuteRootCommand(string command);

        /// <summary>
        /// 异步执行需要 root 权限的指令。
        /// </summary>
        /// <param name="command">需要 root 权限的指令。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        Task ExecuteRootCommandAsync(string command, CancellationToken cancellationToken = default);
    }
}
