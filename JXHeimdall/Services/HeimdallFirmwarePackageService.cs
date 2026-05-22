using JXHeimdall.Models;
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
            { "modem.bin", "RADIO" },
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

            var workingDirectory = Path.Combine(Path.GetTempPath(), "JXHeimdall", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);

            var package = new HeimdallFirmwarePackageModel
            {
                Slot = slot,
                SourceFilePath = sourceFilePath,
                WorkingDirectory = workingDirectory
            };

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
                    if (string.IsNullOrWhiteSpace(entryName))
                    {
                        SkipTarContent(stream, size);
                        continue;
                    }

                    var safeFileName = Path.GetFileName(entryName);
                    var suggestedPartition = SuggestPartitionName(safeFileName);
                    var targetPath = Path.Combine(workingDirectory, safeFileName);
                    using (var output = File.Create(targetPath))
                    {
                        CopyExactly(stream, output, size);
                    }

                    SkipPadding(stream, size);

                    if (!string.IsNullOrWhiteSpace(suggestedPartition))
                    {
                        package.Files.Add(new HeimdallFirmwareFileModel
                        {
                            EntryName = safeFileName,
                            ExtractedFilePath = targetPath,
                            Size = size,
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

        private static bool IsEmptyHeader(byte[] header)
        {
            return header.All(value => value == 0);
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
