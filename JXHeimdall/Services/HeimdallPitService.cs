using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责读取并解析 Samsung 设备 PIT 分区表。
    /// </summary>
    public sealed class HeimdallPitService
    {
        private static readonly Regex EntryRegex = new Regex(@"--- Entry #(?<index>\d+) ---", RegexOptions.Compiled);
        private readonly HeimdallProcessService _processService;

        /// <summary>
        /// 初始化 PIT 服务。
        /// </summary>
        /// <param name="processService">Heimdall 进程服务。</param>
        public HeimdallPitService(HeimdallProcessService processService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        }

        /// <summary>
        /// 读取当前 Download 设备的 PIT 分区信息。
        /// </summary>
        /// <param name="log">日志回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>PIT 分区集合。</returns>
        public async Task<IReadOnlyList<HeimdallPitPartitionModel>> ReadPartitionsAsync(Action<string> log, CancellationToken cancellationToken)
        {
            var result = await _processService.ExecuteAsync("print-pit --no-reboot", log, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException("读取 PIT 失败：" + result.StandardError);
            }

            return ParsePartitions(result.StandardOutput + Environment.NewLine + result.StandardError);
        }

        /// <summary>
        /// 从 Heimdall 输出内容解析 PIT 分区列表。
        /// </summary>
        /// <param name="pitOutput">print-pit 输出内容。</param>
        /// <returns>PIT 分区列表。</returns>
        public IReadOnlyList<HeimdallPitPartitionModel> ParsePartitions(string pitOutput)
        {
            var partitions = new List<HeimdallPitPartitionModel>();
            HeimdallPitPartitionModel current = null;
            foreach (var rawLine in (pitOutput ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                var entryMatch = EntryRegex.Match(line);
                if (entryMatch.Success)
                {
                    current = new HeimdallPitPartitionModel
                    {
                        Index = int.Parse(entryMatch.Groups["index"].Value)
                    };
                    partitions.Add(current);
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                if (line.StartsWith("Partition Name:", StringComparison.OrdinalIgnoreCase))
                {
                    current.PartitionName = line.Substring("Partition Name:".Length).Trim();
                }
                else if (line.StartsWith("Filename:", StringComparison.OrdinalIgnoreCase))
                {
                    current.FileName = line.Substring("Filename:".Length).Trim();
                }
            }

            return partitions;
        }
    }
}
