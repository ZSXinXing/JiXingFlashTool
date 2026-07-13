using JiXingFlashTool.Extensions;
using JiXingFlashTool.Model;
using JiXingFlashTool.Repositorys;
using JiXingFlashTool.Utils;
using SQLCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    /// <summary>
    /// 资源路径服务，负责缓存、持久化资源路径并刷新 ROM 包提取工具。
    /// </summary>
    public sealed class ResourceFilePathService
    {
        /// <summary>
        /// 系统固件资源类型标识。
        /// </summary>
        public const string SystemFirmwareResourceType = "SystemFirmware";

        /// <summary>
        /// TWRP 资源类型标识。
        /// </summary>
        public const string TwrpResourceType = "Twrp";

        private readonly object _syncRoot = new object();
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private Dictionary<string, ResourceFilePathModel> _resourcePaths = new Dictionary<string, ResourceFilePathModel>(StringComparer.OrdinalIgnoreCase);
        private bool _isInitialized;

        /// <summary>
        /// 初始化资源路径服务。
        /// </summary>
        private ResourceFilePathService()
        {
        }

        /// <summary>
        /// 资源路径服务单例。
        /// </summary>
        public static ResourceFilePathService Instance { get; } = new ResourceFilePathService();

        /// <summary>
        /// 初始化资源路径数据库并读取已保存路径到内存缓存。
        /// </summary>
        /// <returns>异步初始化任务。</returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            await _initializeLock.WaitAsync();
            try
            {
                if (_isInitialized)
                {
                    return;
                }

                await SQLService.Instance.InitializeAsync();

                var entities = await GetRepository().GetAllAsync();
                UpdateCache(entities.Select(entity => entity.ToModel()));
                _isInitialized = true;
                RefreshRomUtil();
            }
            catch (Exception exception)
            {
                WriteErrorLog("InitializeAsync", exception);
                throw;
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        /// <summary>
        /// 获取指定资源类型和设备系列的已保存路径。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="seriesName">设备系列名称。</param>
        /// <returns>已保存路径；没有记录时返回空字符串。</returns>
        public string GetFilePath(string resourceType, string seriesName)
        {
            string key = CreateResourceKey(resourceType, seriesName);
            lock (_syncRoot)
            {
                return _resourcePaths.TryGetValue(key, out ResourceFilePathModel model) ? model.FilePath : string.Empty;
            }
        }

        /// <summary>
        /// 事务保存资源管理弹窗中的全部资源路径，并同步刷新 ROM 包提取工具。
        /// </summary>
        /// <param name="models">当前已选择的资源路径集合。</param>
        /// <returns>异步保存任务。</returns>
        public async Task SaveAsync(IEnumerable<ResourceFilePathModel> models)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            List<ResourceFilePathModel> normalizedModels = NormalizeModels(models).ToList();
            try
            {
                await GetRepository().ReplaceAllAsync(normalizedModels.Select(model => model.ToEntity()));
                UpdateCache(normalizedModels);
                RefreshRomUtil();
            }
            catch (Exception exception)
            {
                WriteErrorLog("SaveAsync", exception);
                throw;
            }
        }

        /// <summary>
        /// 从当前缓存中生成可供 ROM 包提取工具使用的完整资源组。
        /// </summary>
        /// <returns>同时具备 ROM 与 TWRP 有效文件的设备系列资源组。</returns>
        public IReadOnlyList<RomPackageArchiveSource> GetRomPackageArchiveSources()
        {
            var sources = new List<RomPackageArchiveSource>();
            foreach (string series in new[] { "S7", "S8", "S9" })
            {
                string romPath = GetFilePath(SystemFirmwareResourceType, series);
                string twrpPath = GetFilePath(TwrpResourceType, series);
                if (!File.Exists(romPath) || !File.Exists(twrpPath))
                {
                    continue;
                }

                sources.Add(new RomPackageArchiveSource
                {
                    Series = series,
                    RomPackagePath = romPath,
                    TwrpPackagePath = twrpPath
                });
            }

            return sources;
        }

        /// <summary>
        /// 获取资源路径仓储实例。
        /// </summary>
        /// <returns>资源路径仓储。</returns>
        private static ResourceFilePathRepository GetRepository()
        {
            return DataBootstrap.Db.Repos.Get<ResourceFilePathRepository>();
        }

        /// <summary>
        /// 标准化资源路径模型并忽略已清空的路径。
        /// </summary>
        /// <param name="models">待标准化的资源路径模型。</param>
        /// <returns>可持久化的资源路径模型集合。</returns>
        private static IEnumerable<ResourceFilePathModel> NormalizeModels(IEnumerable<ResourceFilePathModel> models)
        {
            return (models ?? Enumerable.Empty<ResourceFilePathModel>())
                .Where(model => model != null && !string.IsNullOrWhiteSpace(model.FilePath))
                .Select(model => new ResourceFilePathModel
                {
                    ResourceType = model.ResourceType?.Trim(),
                    Series = NormalizeSeries(model.Series),
                    FilePath = Path.GetFullPath(model.FilePath),
                    UpdatedAt = model.UpdatedAt == default ? DateTime.Now : model.UpdatedAt
                })
                .Where(model => !string.IsNullOrWhiteSpace(model.ResourceType) && !string.IsNullOrWhiteSpace(model.Series))
                .GroupBy(model => CreateResourceKey(model.ResourceType, model.Series), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last());
        }

        /// <summary>
        /// 使用最新模型集合替换内存缓存。
        /// </summary>
        /// <param name="models">资源路径模型集合。</param>
        private void UpdateCache(IEnumerable<ResourceFilePathModel> models)
        {
            Dictionary<string, ResourceFilePathModel> cache = (models ?? Enumerable.Empty<ResourceFilePathModel>())
                .Where(model => model != null && !string.IsNullOrWhiteSpace(model.ResourceType) && !string.IsNullOrWhiteSpace(model.Series))
                .ToDictionary(model => CreateResourceKey(model.ResourceType, model.Series), model => model, StringComparer.OrdinalIgnoreCase);
            lock (_syncRoot)
            {
                _resourcePaths = cache;
            }
        }

        /// <summary>
        /// 按当前缓存重新初始化 ROM 包提取工具，并清除旧解压缓存。
        /// </summary>
        private void RefreshRomUtil()
        {
            RomUtil.Instance.ReloadPackageSources(GetRomPackageArchiveSources());
        }

        /// <summary>
        /// 生成资源路径缓存与数据库主键。
        /// </summary>
        /// <param name="resourceType">资源类型。</param>
        /// <param name="seriesName">设备系列名称。</param>
        /// <returns>资源唯一键。</returns>
        private static string CreateResourceKey(string resourceType, string seriesName)
        {
            return $"{resourceType?.Trim()}:{NormalizeSeries(seriesName)}";
        }

        /// <summary>
        /// 将界面系列名称规范为 ROM 包使用的系列标识。
        /// </summary>
        /// <param name="seriesName">设备系列名称。</param>
        /// <returns>规范化后的系列标识。</returns>
        private static string NormalizeSeries(string seriesName)
        {
            return (seriesName ?? string.Empty).Replace("系列", string.Empty).Trim();
        }

        /// <summary>
        /// 将资源路径持久化异常写入调试日志。
        /// </summary>
        /// <param name="methodName">发生异常的方法名称。</param>
        /// <param name="exception">异常对象。</param>
        private static void WriteErrorLog(string methodName, Exception exception)
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log");
                Directory.CreateDirectory(logDirectory);
                string logPath = Path.Combine(logDirectory, $"resource_path_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ERROR ResourceFilePathService.{methodName}{Environment.NewLine}{exception}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
