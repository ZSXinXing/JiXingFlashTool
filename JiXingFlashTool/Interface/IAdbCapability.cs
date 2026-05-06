using System.Threading;
using System.Threading.Tasks;
using TaskCore.Abstractions;

namespace JiXingFlashTool.Interface
{
    /// <summary>
    /// ADB 能力接口，供 TaskCore 任务通过会话获取并使用。
    /// </summary>
    public interface IAdbCapability : IDeviceCapability
    {
        /// <summary>
        /// 执行同步 ADB 命令。
        /// </summary>
        /// <param name="command">命令内容。</param>
        /// <returns>命令输出。</returns>
        string ExecuteRemoteCommand(string command);

        /// <summary>
        /// 执行异步 ADB 命令。
        /// </summary>
        /// <param name="command">命令内容。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>异步任务。</returns>
        Task ExecuteRemoteCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统属性。
        /// </summary>
        /// <param name="propKey">属性键名。</param>
        /// <returns>属性值。</returns>
        string GetProp(string propKey);
    }
}
