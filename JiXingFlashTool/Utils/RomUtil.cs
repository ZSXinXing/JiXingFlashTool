using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Utils
{
    /// <summary>
    /// 根据设备主板代号从本地加密 ROM 与 TWRP 包中提取匹配文件。
    /// </summary>
    internal sealed class RomUtil
    {
        private const string RomPackagePassword = "pDGMjcs6Qgb3AMfEDwGfIy-86YkgNZA5G1bRuk2fyKQLRxbq";
        private const string TwrpPackagePassword = "nvcoWS3zUIGXbFwh3HMi4D4boFq6T4rqaBshMW02MuLik3XI";
        private const string ManifestFileName = "data.json";
        private static readonly Lazy<RomUtil> LazyInstance = new Lazy<RomUtil>(() => new RomUtil());
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, RomPackageExtractionResult> _extractedResults = new Dictionary<string, RomPackageExtractionResult>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _resolvedBoards = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<RomPackageArchiveSource> _packageSources = Array.Empty<RomPackageArchiveSource>();

        /// <summary>
        /// ROM 包提取工具单例。
        /// </summary>
        public static RomUtil Instance => LazyInstance.Value;

        /// <summary>
        /// 初始化当前设备系列对应的 ROM 与 TWRP 加密压缩包。
        /// </summary>
        /// <param name="packageSources">S7、S8、S9 等设备系列的资源包路径集合。</param>
        public void Initialize(IEnumerable<RomPackageArchiveSource> packageSources)
        {
            var normalizedSources = (packageSources ?? Enumerable.Empty<RomPackageArchiveSource>())
                .Select(NormalizePackageSource)
                .ToList();
            if (normalizedSources.Count == 0)
            {
                throw new ArgumentException("至少需要配置一组 ROM 与 TWRP 压缩包路径。", nameof(packageSources));
            }

            if (normalizedSources.GroupBy(item => item.Series, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                throw new ArgumentException("设备系列不能重复配置。", nameof(packageSources));
            }

            UpdatePackageSources(normalizedSources);
        }

        /// <summary>
        /// 使用已保存资源路径刷新 ROM 与 TWRP 压缩包配置，允许清空全部配置。
        /// </summary>
        /// <param name="packageSources">当前可用的设备系列资源包路径集合。</param>
        public void ReloadPackageSources(IEnumerable<RomPackageArchiveSource> packageSources)
        {
            var normalizedSources = (packageSources ?? Enumerable.Empty<RomPackageArchiveSource>())
                .Select(NormalizePackageSource)
                .ToList();
            if (normalizedSources.GroupBy(item => item.Series, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                throw new ArgumentException("设备系列不能重复配置。", nameof(packageSources));
            }

            UpdatePackageSources(normalizedSources);
        }

        /// <summary>
        /// 更新资源包配置并清空依赖旧路径的提取缓存。
        /// </summary>
        /// <param name="normalizedSources">已校验的资源包路径集合。</param>
        private void UpdatePackageSources(IReadOnlyList<RomPackageArchiveSource> normalizedSources)
        {
            lock (_syncRoot)
            {
                if (ArePackageSourcesEqual(_packageSources, normalizedSources))
                {
                    return;
                }

                _packageSources = normalizedSources;
                _extractedResults.Clear();
                _resolvedBoards.Clear();
            }
        }

        /// <summary>
        /// 异步提取指定设备代号对应的 ROM 与 TWRP 文件。
        /// </summary>
        /// <param name="deviceBoard">设备主板代号。</param>
        /// <param name="romTargetDirectory">ROM 文件目标目录。</param>
        /// <param name="twrpTargetDirectory">TWRP 文件目标目录。</param>
        /// <returns>ROM 与 TWRP 的提取结果。</returns>
        public Task<RomPackageExtractionResult> ExtractPackagesAsync(string deviceBoard, string romTargetDirectory, string twrpTargetDirectory)
        {
            return Task.Run(() => ExtractPackages(deviceBoard, romTargetDirectory, twrpTargetDirectory));
        }

        /// <summary>
        /// 提取指定设备代号对应的 ROM 与 TWRP 文件。
        /// </summary>
        /// <param name="deviceBoard">设备主板代号。</param>
        /// <param name="romTargetDirectory">ROM 文件目标目录。</param>
        /// <param name="twrpTargetDirectory">TWRP 文件目标目录。</param>
        /// <returns>ROM 与 TWRP 的提取结果。</returns>
        public RomPackageExtractionResult ExtractPackages(string deviceBoard, string romTargetDirectory, string twrpTargetDirectory)
        {
            string normalizedBoard = NormalizeBoard(deviceBoard);
            if (string.IsNullOrWhiteSpace(normalizedBoard))
            {
                throw new ArgumentException("设备主板代号不能为空。", nameof(deviceBoard));
            }

            if (string.IsNullOrWhiteSpace(romTargetDirectory))
            {
                throw new ArgumentException("ROM 目标目录不能为空。", nameof(romTargetDirectory));
            }

            if (string.IsNullOrWhiteSpace(twrpTargetDirectory))
            {
                throw new ArgumentException("TWRP 目标目录不能为空。", nameof(twrpTargetDirectory));
            }

            lock (_syncRoot)
            {
                EnsureInitialized();
                string cacheKey = _resolvedBoards.TryGetValue(normalizedBoard, out string resolvedBoard)
                    ? resolvedBoard
                    : normalizedBoard;
                if (_extractedResults.TryGetValue(cacheKey, out RomPackageExtractionResult extractedResult))
                {
                    return extractedResult;
                }

                var result = new RomPackageExtractionResult
                {
                    RequestedBoard = normalizedBoard
                };
                ExtractRomPackage(normalizedBoard, romTargetDirectory, result);
                ExtractTwrpPackage(normalizedBoard, twrpTargetDirectory, result);
                if (result.HasRom && result.HasTwrp)
                {
                    string actualBoard = NormalizeBoard(result.RomBoard);
                    _resolvedBoards[normalizedBoard] = actualBoard;
                    _resolvedBoards[actualBoard] = actualBoard;
                    _extractedResults[actualBoard] = result;
                }

                return result;
            }
        }

        /// <summary>
        /// 查找并提取 ROM 文件。
        /// </summary>
        private void ExtractRomPackage(string deviceBoard, string targetDirectory, RomPackageExtractionResult result)
        {
            try
            {
                ArchivePackageMatch match = FindRomPackage(deviceBoard);
                result.RomBoard = match.Board;
                result.RomPackageVersion = match.PackageVersion;
                result.RomBuildDate = match.BuildDate;
                result.RomFilePath = ExtractArchiveFile(match, RomPackagePassword, targetDirectory);
            }
            catch (Exception exception)
            {
                result.RomError = exception.Message;
            }
        }

        /// <summary>
        /// 查找并提取 TWRP 文件。
        /// </summary>
        private void ExtractTwrpPackage(string deviceBoard, string targetDirectory, RomPackageExtractionResult result)
        {
            try
            {
                ArchivePackageMatch match = FindTwrpPackage(deviceBoard);
                result.TwrpBoard = match.Board;
                result.TwrpBuildDate = match.BuildDate;
                result.TwrpFilePath = ExtractArchiveFile(match, TwrpPackagePassword, targetDirectory);
            }
            catch (Exception exception)
            {
                result.TwrpError = exception.Message;
            }
        }

        /// <summary>
        /// 从 ROM 包清单中查找指定设备代号。
        /// </summary>
        private ArchivePackageMatch FindRomPackage(string deviceBoard)
        {
            var matches = new List<ArchivePackageMatch>();
            foreach (RomPackageArchiveSource source in _packageSources)
            {
                RomPackageManifest manifest = ReadManifest<RomPackageManifest>(source.RomPackagePath, RomPackagePassword);
                foreach (RomFirmwarePackage package in manifest.FirmwarePackages ?? Enumerable.Empty<RomFirmwarePackage>())
                {
                    if (IsBoardMatched(deviceBoard, package.Board, package.CompatibleBoards))
                    {
                        matches.Add(new ArchivePackageMatch(source.RomPackagePath, package.FirmwareFile, package.Board, package.BuildDate, package.PackageVersion));
                    }
                }
            }

            return SelectBestMatch(deviceBoard, matches, "ROM");
        }

        /// <summary>
        /// 从 TWRP 包清单中查找指定设备代号。
        /// </summary>
        private ArchivePackageMatch FindTwrpPackage(string deviceBoard)
        {
            var matches = new List<ArchivePackageMatch>();
            foreach (RomPackageArchiveSource source in _packageSources)
            {
                TwrpPackageManifest manifest = ReadManifest<TwrpPackageManifest>(source.TwrpPackagePath, TwrpPackagePassword);
                foreach (TwrpRecoveryImage image in manifest.RecoveryImages ?? Enumerable.Empty<TwrpRecoveryImage>())
                {
                    if (IsBoardMatched(deviceBoard, image.Board, image.CompatibleBoards))
                    {
                        matches.Add(new ArchivePackageMatch(source.TwrpPackagePath, image.ImageFile, image.Board, image.BuildDate, string.Empty));
                    }
                }
            }

            return SelectBestMatch(deviceBoard, matches, "TWRP");
        }

        /// <summary>
        /// 确认当前实例已完成压缩包初始化。
        /// </summary>
        private void EnsureInitialized()
        {
            if (_packageSources.Count == 0)
            {
                throw new InvalidOperationException("请先初始化 ROM 与 TWRP 加密压缩包路径。");
            }
        }

        /// <summary>
        /// 校验并规范化单个设备系列的资源包路径。
        /// </summary>
        private static RomPackageArchiveSource NormalizePackageSource(RomPackageArchiveSource source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.Series))
            {
                throw new ArgumentException("设备系列不能为空。", nameof(source));
            }

            if (string.IsNullOrWhiteSpace(source.RomPackagePath) || !File.Exists(source.RomPackagePath))
            {
                throw new FileNotFoundException($"{source.Series} 系列的 ROM 加密压缩包不存在。", source.RomPackagePath);
            }

            if (string.IsNullOrWhiteSpace(source.TwrpPackagePath) || !File.Exists(source.TwrpPackagePath))
            {
                throw new FileNotFoundException($"{source.Series} 系列的 TWRP 加密压缩包不存在。", source.TwrpPackagePath);
            }

            return new RomPackageArchiveSource
            {
                Series = source.Series.Trim(),
                RomPackagePath = Path.GetFullPath(source.RomPackagePath),
                TwrpPackagePath = Path.GetFullPath(source.TwrpPackagePath)
            };
        }

        /// <summary>
        /// 判断新旧资源包配置是否一致。
        /// </summary>
        private static bool ArePackageSourcesEqual(IReadOnlyList<RomPackageArchiveSource> currentSources, IReadOnlyList<RomPackageArchiveSource> newSources)
        {
            if (currentSources.Count != newSources.Count)
            {
                return false;
            }

            return currentSources.All(currentSource => newSources.Any(newSource =>
                string.Equals(currentSource.Series, newSource.Series, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentSource.RomPackagePath, newSource.RomPackagePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentSource.TwrpPackagePath, newSource.TwrpPackagePath, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// 从压缩包中读取 JSON 清单。
        /// </summary>
        private static TManifest ReadManifest<TManifest>(string archivePath, string password) where TManifest : class
        {
            string json = ExecuteSevenZip($"e -so -bso0 -bsp0 -bse0 -p{password} -- {QuoteArgument(archivePath)} {ManifestFileName}");
            byte[] jsonBytes = Encoding.UTF8.GetBytes((json ?? string.Empty).TrimStart('\uFEFF'));
            using (var stream = new MemoryStream(jsonBytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(TManifest));
                var manifest = serializer.ReadObject(stream) as TManifest;
                if (manifest == null)
                {
                    throw new InvalidOperationException($"固件包清单无效: {archivePath}");
                }

                return manifest;
            }
        }

        /// <summary>
        /// 从匹配的压缩包中提取指定文件；目标文件已存在时不覆盖。
        /// </summary>
        private static string ExtractArchiveFile(ArchivePackageMatch match, string password, string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(match.FileName) || !string.Equals(Path.GetFileName(match.FileName), match.FileName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("固件包清单中的文件名无效。");
            }

            Directory.CreateDirectory(targetDirectory);
            string outputPath = Path.Combine(targetDirectory, match.FileName);
            if (File.Exists(outputPath))
            {
                return outputPath;
            }

            ExecuteSevenZip($"e -y -aos -bso0 -bsp0 -bse0 -p{password} -o{QuoteArgument(targetDirectory)} -- {QuoteArgument(match.ArchivePath)} {QuoteArgument(match.FileName)}");
            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException($"压缩包中未提取到目标文件: {match.FileName}");
            }

            return outputPath;
        }

        /// <summary>
        /// 从候选项中优先选择主板代号完全匹配的文件。
        /// </summary>
        private static ArchivePackageMatch SelectBestMatch(string deviceBoard, List<ArchivePackageMatch> matches, string packageType)
        {
            ArchivePackageMatch match = matches.FirstOrDefault(item => string.Equals(item.Board, deviceBoard, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault();
            if (match == null)
            {
                throw new FileNotFoundException($"未找到设备代号 {deviceBoard} 对应的 {packageType} 包。");
            }

            return match;
        }

        /// <summary>
        /// 判断设备代号是否匹配清单中的主板或兼容代号。
        /// </summary>
        private static bool IsBoardMatched(string deviceBoard, string board, IEnumerable<string> compatibleBoards)
        {
            return string.Equals(deviceBoard, NormalizeBoard(board), StringComparison.OrdinalIgnoreCase) ||
                   (compatibleBoards ?? Enumerable.Empty<string>()).Any(item => string.Equals(deviceBoard, NormalizeBoard(item), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 规范化设备主板代号。
        /// </summary>
        private static string NormalizeBoard(string deviceBoard)
        {
            return (deviceBoard ?? string.Empty).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 执行 7-Zip 并获取标准输出。
        /// </summary>
        private static string ExecuteSevenZip(string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = FindSevenZipExecutable();
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.Start();

                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"7-Zip 执行失败，退出代码: {process.ExitCode}。{standardError}".Trim());
                }

                return standardOutput;
            }
        }

        /// <summary>
        /// 查找本机可用的 7-Zip 可执行文件。
        /// </summary>
        private static string FindSevenZipExecutable()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.exe"),
                "7z.exe"
            };

            foreach (string candidate in candidates)
            {
                if (string.Equals(candidate, "7z.exe", StringComparison.OrdinalIgnoreCase) || File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException("未找到 7z.exe，请安装 7-Zip 后重试。");
        }

        /// <summary>
        /// 转义 7-Zip 命令行参数。
        /// </summary>
        private static string QuoteArgument(string value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";
        }

        /// <summary>
        /// 压缩包内已匹配文件的信息。
        /// </summary>
        private sealed class ArchivePackageMatch
        {
            /// <summary>
            /// 使用压缩包及文件信息初始化匹配项。
            /// </summary>
            public ArchivePackageMatch(string archivePath, string fileName, string board, string buildDate, string packageVersion)
            {
                ArchivePath = archivePath;
                FileName = fileName;
                Board = board;
                BuildDate = buildDate;
                PackageVersion = packageVersion;
            }

            /// <summary>压缩包路径。</summary>
            public string ArchivePath { get; }

            /// <summary>压缩包内文件名。</summary>
            public string FileName { get; }

            /// <summary>实际主板代号。</summary>
            public string Board { get; }

            /// <summary>编译日期。</summary>
            public string BuildDate { get; }

            /// <summary>ROM 包版本。</summary>
            public string PackageVersion { get; }
        }

    }
}
