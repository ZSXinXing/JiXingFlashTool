using LanguageCore;
using System;
using System.Linq;
using TaskCore.Tasks;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// 任务日志本地化服务，负责在不修改 TaskCore.TaskLog 的前提下保存语言键并按当前语言解析显示文案。
    /// </summary>
    public static class TaskLogLocalizationService
    {
        private const string TokenPrefix = "__JIXING_TASK_LOC__|";

        /// <summary>
        /// 创建可随语言切换重新解析的任务日志。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        /// <returns>携带本地化标记的任务日志。</returns>
        public static TaskLog CreateLog(string key, params object[] args)
        {
            return new TaskLog(CreateToken(key, args), "Info", DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// 将 TaskCore 固定格式日志转换为可随语言切换刷新的本地化标记。
        /// </summary>
        /// <param name="message">TaskCore 输出的原始日志消息。</param>
        /// <returns>可本地化的日志标记或原始消息。</returns>
        public static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.StartsWith(TokenPrefix, StringComparison.Ordinal))
            {
                return message ?? string.Empty;
            }

            const string startedPrefix = "任务开始：";
            const string completedPrefix = "任务完成：";
            const string canceledPrefix = "任务已取消：";

            if (string.Equals(message, "Error", StringComparison.OrdinalIgnoreCase))
            {
                return CreateToken("TaskLog_TaskFailed", Array.Empty<object>());
            }

            if (message.StartsWith(startedPrefix, StringComparison.Ordinal))
            {
                return CreateToken("TaskLog_TaskStarted", new object[] { message.Substring(startedPrefix.Length) });
            }

            if (message.StartsWith(completedPrefix, StringComparison.Ordinal))
            {
                return CreateToken("TaskLog_TaskCompleted", new object[] { message.Substring(completedPrefix.Length) });
            }

            if (message.StartsWith(canceledPrefix, StringComparison.Ordinal))
            {
                return CreateToken("TaskLog_TaskCanceled", new object[] { message.Substring(canceledPrefix.Length) });
            }

            return message;
        }

        /// <summary>
        /// 根据当前语言解析任务日志显示文案。
        /// </summary>
        /// <param name="message">TaskCore 传递的原始日志消息。</param>
        /// <returns>当前语言下的显示文案。</returns>
        public static string ResolveMessage(string message)
        {
            if (!TryParseToken(message, out var key, out var args))
            {
                return message ?? string.Empty;
            }

            var template = LocalizationService.Instance.GetString(string.Empty, key);
            if (args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        /// <summary>
        /// 创建可安全存入 TaskLog.Message 的轻量标记。
        /// </summary>
        /// <param name="key">语言资源键。</param>
        /// <param name="args">格式化参数。</param>
        /// <returns>序列化后的日志标记。</returns>
        private static string CreateToken(string key, object[] args)
        {
            var encodedKey = Uri.EscapeDataString(key ?? string.Empty);
            if (args == null || args.Length == 0)
            {
                return TokenPrefix + encodedKey;
            }

            var encodedArgs = args
                .Select(arg => Uri.EscapeDataString(Convert.ToString(arg) ?? string.Empty));
            return TokenPrefix + encodedKey + "|" + string.Join("|", encodedArgs);
        }

        /// <summary>
        /// 尝试从日志消息中解析语言键和格式化参数。
        /// </summary>
        /// <param name="message">原始日志消息。</param>
        /// <param name="key">解析出的语言资源键。</param>
        /// <param name="args">解析出的格式化参数。</param>
        /// <returns>是否成功解析为本地化任务日志。</returns>
        private static bool TryParseToken(string message, out string key, out object[] args)
        {
            key = string.Empty;
            args = Array.Empty<object>();

            if (string.IsNullOrWhiteSpace(message) || !message.StartsWith(TokenPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var payload = message.Substring(TokenPrefix.Length);
            var parts = payload.Split('|');
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                return false;
            }

            key = Uri.UnescapeDataString(parts[0]);
            args = parts
                .Skip(1)
                .Select(part => (object)Uri.UnescapeDataString(part))
                .ToArray();
            return true;
        }
    }
}
