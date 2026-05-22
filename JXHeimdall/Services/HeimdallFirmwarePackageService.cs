using JXHeimdall.Models;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责解析 Odin tar、md5、tar.md5 固件包并提取可刷入文件。
    /// </summary>
    public sealed class HeimdallFirmwarePackageService
    {
        private const string ExtractRootDirectoryName = "JXHeimdallExtract";

        private static readonly Dictionary<string, string> PartitionNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "boot.img", "BOOT" },
            { "recovery.img", "RECOVERY" },
            { "system.img", "SYSTEM" },
            { "vendor.img", "VENDOR" },
            { "product.img", "PRODUCT" },
            { "odm.img", "ODM" },
            { "userdata.img", "USERDATA" },
            { "cache.img", "CACHE" },
            { "modem.bin", "MODEM" },
            { "cm.bin", "CM" },
            { "sboot.bin", "SBOOT" },
            { "vbmeta.img", "VBMETA" },
            { "dtbo.img", "DTBO" },
            { "vendor_boot.img", "VENDOR_BOOT" },
            { "init_boot.img", "INIT_BOOT" }
        };

        /// <summary>
        /// 解析固件包并解包到临时目录。
        /// </summary>
        /// <param name="slot">固件槽位。</param>
        /// <param name="sourceFilePath">固件包路径。</param>
        /// <returns>已解析固件包模型。</returns>
        public HeimdallFirmwarePackageModel ParsePackage(HeimdallFirmwareSlot slot, string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("固件文件不存在。", sourceFilePath);
            }

            var workingDirectory = CreateWorkingDirectory();
            Directory.CreateDirectory(workingDirectory);

            var package = new HeimdallFirmwarePackageModel
            {
                Slot = slot,
                SourceFilePath = sourceFilePath,
                WorkingDirectory = workingDirectory
            };

            if (slot == HeimdallFirmwareSlot.TWRP && IsImageFile(sourceFilePath))
            {
                var targetPath = Path.Combine(workingDirectory, "recovery.img");
                File.Copy(sourceFilePath, targetPath, true);
                package.Files.Add(new HeimdallFirmwareFileModel
                {
                    EntryName = "recovery.img",
                    ExtractedFilePath = targetPath,
                    Size = new FileInfo(targetPath).Length,
                    SuggestedPartitionName = "RECOVERY"
                });
                return package;
            }

            ExtractTarEntries(sourceFilePath, workingDirectory, package);
            if (slot == HeimdallFirmwareSlot.TWRP && package.Files.Count == 0)
            {
                var targetPath = Path.Combine(workingDirectory, Path.GetFileName(sourceFilePath));
                File.Copy(sourceFilePath, targetPath, true);
                package.Files.Add(new HeimdallFirmwareFileModel
                {
                    EntryName = Path.GetFileName(sourceFilePath),
                    ExtractedFilePath = targetPath,
                    Size = new FileInfo(targetPath).Length,
                    SuggestedPartitionName = "RECOVERY"
                });
            }

            return package;
        }

        /// <summary>
        /// 清理应用运行目录下的 Heimdall 固件解包目录。
        /// </summary>
        public static void CleanupExtractRootDirectory()
        {
            DeleteDirectoryQuietly(GetExtractRootDirectory());
        }

        /// <summary>
        /// 清理本次刷入解析出的固件包工作目录，避免刷机成功后继续占用大量磁盘空间。
        /// </summary>
        /// <param name="packages">本次刷入解析出的固件包集合。</param>
        public static void CleanupPackageWorkingDirectories(IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            if (packages == null)
            {
                return;
            }

            foreach (var package in packages)
            {
                if (!string.IsNullOrWhiteSpace(package?.WorkingDirectory))
                {
                    DeleteDirectoryQuietly(package.WorkingDirectory);
                }
            }
        }

        /// <summary>
        /// 创建固件解包工作目录，目录固定在当前应用运行目录下。
        /// </summary>
        /// <returns>可用于本次解包的临时工作目录。</returns>
        private static string CreateWorkingDirectory()
        {
            return Path.Combine(GetExtractRootDirectory(), Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// 获取当前应用运行目录下的 Heimdall 固件解包根目录。
        /// </summary>
        /// <returns>解包根目录路径。</returns>
        private static string GetExtractRootDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtractRootDirectoryName);
        }

        /// <summary>
        /// 安静删除指定目录，清理失败时写入调试日志但不中断主流程。
        /// </summary>
        /// <param name="directoryPath">待删除目录路径。</param>
        private static void DeleteDirectoryQuietly(string directoryPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("Package", "delete-extract-directory-failed Path=" + directoryPath + " Error=" + ex.Message);
            }
        }

        /// <summary>
        /// 将 AP 包中的 recovery 刷入文件替换为指定的 TWRP 镜像。
        /// </summary>
        /// <param name="package">已解析的 AP 固件包。</param>
        /// <param name="twrpFilePath">用于替换 recovery 的 TWRP 镜像路径。</param>
        public void ApplyRecoveryOverride(HeimdallFirmwarePackageModel package, string twrpFilePath)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (string.IsNullOrWhiteSpace(twrpFilePath))
            {
                return;
            }

            if (!File.Exists(twrpFilePath))
            {
                throw new FileNotFoundException("TWRP 镜像文件不存在。", twrpFilePath);
            }

            var targetPath = Path.Combine(package.WorkingDirectory, "recovery_override.img");
            File.Copy(twrpFilePath, targetPath, true);
            var recoveryFile = package.Files.FirstOrDefault(item =>
                item.EntryName.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(item.SuggestedPartitionName, "RECOVERY", StringComparison.OrdinalIgnoreCase));

            if (recoveryFile == null)
            {
                package.Files.Add(new HeimdallFirmwareFileModel
                {
                    EntryName = "recovery.img",
                    ExtractedFilePath = targetPath,
                    Size = new FileInfo(targetPath).Length,
                    SuggestedPartitionName = "RECOVERY"
                });
                HeimdallDebugLogService.Write("Package", "add-recovery-override File=" + targetPath);
                return;
            }

            recoveryFile.EntryName = "recovery.img";
            recoveryFile.ExtractedFilePath = targetPath;
            recoveryFile.Size = new FileInfo(targetPath).Length;
            recoveryFile.SuggestedPartitionName = "RECOVERY";
            HeimdallDebugLogService.Write("Package", "apply-recovery-override File=" + targetPath);
        }

        /// <summary>
        /// 判断源文件是否为可直接刷入 Recovery 分区的 img 镜像。
        /// </summary>
        /// <param name="sourceFilePath">源文件路径。</param>
        /// <returns>文件扩展名为 .img 时返回 true。</returns>
        private static bool IsImageFile(string sourceFilePath)
        {
            return string.Equals(Path.GetExtension(sourceFilePath), ".img", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 根据 PIT 分区表过滤固件包内真正可刷入的文件。
        /// </summary>
        /// <param name="package">固件包。</param>
        /// <param name="partitions">PIT 分区集合。</param>
        /// <returns>分区名与文件路径映射。</returns>
        public IReadOnlyDictionary<string, string> BuildFlashMap(HeimdallFirmwarePackageModel package, IReadOnlyList<HeimdallPitPartitionModel> partitions)
        {
            var availablePartitions = new HashSet<string>(
                partitions.Select(item => item.PartitionName).Where(item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var firmwareFile in package.Files)
            {
                var partitionName = firmwareFile.SuggestedPartitionName;
                if (string.IsNullOrWhiteSpace(partitionName) || !availablePartitions.Contains(partitionName))
                {
                    continue;
                }

                result[partitionName] = firmwareFile.ExtractedFilePath;
            }

            return result;
        }

        /// <summary>
        /// 根据 PIT 分区表构造 CP/Modem 基带固件刷入映射，兼容 MODEM 与 RADIO 分区命名。
        /// </summary>
        /// <param name="package">CP 固件包。</param>
        /// <param name="partitions">PIT 分区集合。</param>
        /// <returns>分区名与文件路径映射。</returns>
        public IReadOnlyDictionary<string, string> BuildCpFlashMap(HeimdallFirmwarePackageModel package, IReadOnlyList<HeimdallPitPartitionModel> partitions)
        {
            var availablePartitions = new HashSet<string>(
                partitions.Select(item => item.PartitionName).Where(item => !string.IsNullOrWhiteSpace(item)),
                StringComparer.OrdinalIgnoreCase);
            var modemFile = package.Files.FirstOrDefault(item =>
                item.EntryName.IndexOf("modem", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(item.SuggestedPartitionName, "MODEM", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.SuggestedPartitionName, "RADIO", StringComparison.OrdinalIgnoreCase));

            if (modemFile == null)
            {
                return BuildFlashMap(package, partitions);
            }

            if (availablePartitions.Contains("MODEM"))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MODEM", modemFile.ExtractedFilePath }
                };
            }

            if (availablePartitions.Contains("RADIO"))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "RADIO", modemFile.ExtractedFilePath }
                };
            }

            return BuildFlashMap(package, partitions);
        }

        /// <summary>
        /// 构造 TWRP 刷入映射，优先选择 recovery 镜像。
        /// </summary>
        /// <param name="package">TWRP 固件包。</param>
        /// <param name="partitions">PIT 分区集合。</param>
        /// <returns>TWRP 分区映射。</returns>
        public IReadOnlyDictionary<string, string> BuildTwrpFlashMap(HeimdallFirmwarePackageModel package, IReadOnlyList<HeimdallPitPartitionModel> partitions)
        {
            var availablePartitions = new HashSet<string>(partitions.Select(item => item.PartitionName), StringComparer.OrdinalIgnoreCase);
            var recoveryFile = package.Files.FirstOrDefault(item =>
                item.EntryName.IndexOf("recovery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                string.Equals(item.SuggestedPartitionName, "RECOVERY", StringComparison.OrdinalIgnoreCase));

            if (recoveryFile != null && availablePartitions.Contains("RECOVERY"))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "RECOVERY", recoveryFile.ExtractedFilePath }
                };
            }

            var bootFile = package.Files.FirstOrDefault(item => string.Equals(item.SuggestedPartitionName, "BOOT", StringComparison.OrdinalIgnoreCase));
            if (bootFile != null && availablePartitions.Contains("BOOT"))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "BOOT", bootFile.ExtractedFilePath }
                };
            }

            throw new InvalidOperationException("未在 TWRP/AP 包中找到可刷入的 recovery 或 boot 镜像。");
        }

        private static void ExtractTarEntries(string sourceFilePath, string workingDirectory, HeimdallFirmwarePackageModel package)
        {
            using (var stream = File.OpenRead(sourceFilePath))
            {
                while (stream.Position + 512 <= stream.Length)
                {
                    var header = new byte[512];
                    if (stream.Read(header, 0, header.Length) != header.Length || IsEmptyHeader(header))
                    {
                        break;
                    }

                    var entryName = ReadString(header, 0, 100);
                    var sizeText = ReadString(header, 124, 12).Trim();
                    var size = string.IsNullOrWhiteSpace(sizeText) ? 0 : Convert.ToInt64(sizeText, 8);
                    if (string.IsNullOrWhiteSpace(entryName) || IsDirectoryEntry(header))
                    {
                        SkipTarContent(stream, size);
                        continue;
                    }

                    var safeFileName = Path.GetFileName(entryName);
                    if (string.IsNullOrWhiteSpace(safeFileName))
                    {
                        SkipTarContent(stream, size);
                        continue;
                    }

                    var targetPath = Path.Combine(workingDirectory, safeFileName);
                    using (var output = File.Create(targetPath))
                    {
                        CopyExactly(stream, output, size);
                    }

                    SkipPadding(stream, size);

                    if (IsPitFile(safeFileName))
                    {
                        package.PitFilePath = targetPath;
                        continue;
                    }

                    var flashFileName = safeFileName;
                    var flashFilePath = targetPath;
                    var flashFileSize = size;
                    if (IsLz4File(safeFileName))
                    {
                        flashFileName = RemoveKnownCompressionSuffix(safeFileName);
                        flashFilePath = DecompressLz4File(targetPath, workingDirectory, flashFileName);
                        flashFileSize = new FileInfo(flashFilePath).Length;
                        TryDeleteTemporaryFile(targetPath);
                    }

                    var suggestedPartition = SuggestPartitionName(flashFileName);
                    if (!string.IsNullOrWhiteSpace(suggestedPartition))
                    {
                        package.Files.Add(new HeimdallFirmwareFileModel
                        {
                            EntryName = flashFileName,
                            ExtractedFilePath = flashFilePath,
                            Size = flashFileSize,
                            SuggestedPartitionName = suggestedPartition
                        });
                    }
                }
            }
        }

        private static string SuggestPartitionName(string fileName)
        {
            var normalizedName = RemoveKnownCompressionSuffix(fileName);
            if (PartitionNameMap.TryGetValue(normalizedName, out var partitionName))
            {
                return partitionName;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedName);
            return string.IsNullOrWhiteSpace(nameWithoutExtension)
                ? string.Empty
                : nameWithoutExtension.ToUpperInvariant();
        }

        private static string RemoveKnownCompressionSuffix(string fileName)
        {
            if (fileName.EndsWith(".lz4", StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Substring(0, fileName.Length - 4);
            }

            return fileName;
        }

        /// <summary>
        /// 判断固件包条目是否为 LZ4 压缩镜像。
        /// </summary>
        /// <param name="fileName">固件包内文件名。</param>
        /// <returns>是 LZ4 文件时返回 true。</returns>
        private static bool IsLz4File(string fileName)
        {
            return fileName.EndsWith(".lz4", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将 Odin 固件包内的 LZ4 压缩镜像解压为 Heimdall 可直接刷入的镜像文件。
        /// </summary>
        /// <param name="sourceFilePath">临时目录中的 LZ4 压缩文件路径。</param>
        /// <param name="workingDirectory">固件包临时工作目录。</param>
        /// <param name="targetFileName">去掉 .lz4 后缀后的目标镜像文件名。</param>
        /// <returns>解压后的镜像文件路径。</returns>
        private static string DecompressLz4File(string sourceFilePath, string workingDirectory, string targetFileName)
        {
            if (string.IsNullOrWhiteSpace(targetFileName))
            {
                throw new InvalidDataException("LZ4 固件条目文件名无效，无法解压。");
            }

            var targetPath = Path.Combine(workingDirectory, targetFileName);
            HeimdallDebugLogService.Write("Package", "decompress-lz4 Source=" + sourceFilePath + " Target=" + targetPath);
            using (var input = File.OpenRead(sourceFilePath))
            using (var lz4Stream = LZ4Stream.Decode(input))
            using (var output = File.Create(targetPath))
            {
                lz4Stream.CopyTo(output);
            }

            return targetPath;
        }

        /// <summary>
        /// 删除已经完成解压的临时压缩文件，避免大体积固件重复占用磁盘空间。
        /// </summary>
        /// <param name="filePath">待删除的临时文件路径。</param>
        private static void TryDeleteTemporaryFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("Package", "delete-temp-lz4-failed File=" + filePath + " Error=" + ex.Message);
            }
        }

        private static bool IsEmptyHeader(byte[] header)
        {
            return header.All(value => value == 0);
        }

        /// <summary>
        /// 判断固件包条目是否为 PIT 分区表文件。
        /// </summary>
        /// <param name="fileName">固件包内文件名。</param>
        /// <returns>是 PIT 文件时返回 true。</returns>
        private static bool IsPitFile(string fileName)
        {
            return fileName.EndsWith(".pit", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断 tar 条目是否为目录，目录项不应创建为刷入文件。
        /// </summary>
        /// <param name="header">tar 条目头。</param>
        /// <returns>是目录项时返回 true。</returns>
        private static bool IsDirectoryEntry(byte[] header)
        {
            return header != null && header.Length > 156 && header[156] == (byte)'5';
        }

        private static string ReadString(byte[] bytes, int offset, int length)
        {
            var end = offset;
            while (end < offset + length && bytes[end] != 0)
            {
                end++;
            }

            return System.Text.Encoding.ASCII.GetString(bytes, offset, end - offset);
        }

        private static void CopyExactly(Stream input, Stream output, long bytesToCopy)
        {
            var buffer = new byte[1024 * 1024];
            var remaining = bytesToCopy;
            while (remaining > 0)
            {
                var readSize = (int)Math.Min(buffer.Length, remaining);
                var read = input.Read(buffer, 0, readSize);
                if (read <= 0)
                {
                    throw new EndOfStreamException("固件包内容不完整。");
                }

                output.Write(buffer, 0, read);
                remaining -= read;
            }
        }

        private static void SkipTarContent(Stream stream, long size)
        {
            stream.Seek(size, SeekOrigin.Current);
            SkipPadding(stream, size);
        }

        private static void SkipPadding(Stream stream, long size)
        {
            var padding = (512 - (size % 512)) % 512;
            if (padding > 0)
            {
                stream.Seek(padding, SeekOrigin.Current);
            }
        }
    }
}
