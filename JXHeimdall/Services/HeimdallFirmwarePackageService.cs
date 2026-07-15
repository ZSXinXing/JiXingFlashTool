using JXHeimdall.Models;
using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JXHeimdall.Services
{
    /// <summary>
    /// 负责解压 Odin TAR/TAR.MD5 固件包，并将 LZ4 条目转换为 Heimdall 可读取的镜像文件。
    /// </summary>
    public sealed class HeimdallFirmwarePackageService
    {
        private const string ExtractRootDirectoryName = "JXHeimdallExtract";

        private static readonly Dictionary<string, string> PartitionNameMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "sboot.bin", "SBOOT" },
                { "cm.bin", "CM" },
                { "param.bin", "PARAM" },
                { "emmc_appsboot.mbn", "ABOOT" },
                { "lksecapp.mbn", "LKSECAPP" },
                { "xbl.elf", "XBL" },
                { "tz.img", "TZ" },
                { "tz.mbn", "TZ" },
                { "hyp.mbn", "HYP" },
                { "devcfg.mbn", "DEVCFG" },
                { "pmic.elf", "PMIC" },
                { "rpm.mbn", "RPM" },
                { "cmnlib.mbn", "CMNLIB" },
                { "cmnlib64.mbn", "CMNLIB64" },
                { "keymaster.mbn", "KEYMASTER" },
                { "apdp.mbn", "APDP" },
                { "msadp.mbn", "MSADP" },
                { "sec.dat", "SEC" },
                { "NON-HLOS.bin", "APNHLOS" },
                { "adspso.bin", "DSP" },
                { "boot.img", "BOOT" },
                { "recovery.img", "RECOVERY" },
                { "system.img", "SYSTEM" },
                { "system.img.ext4", "SYSTEM" },
                { "persist.img.ext4", "PERSIST" },
                { "vendor.img", "VENDOR" },
                { "vendor.img.ext4", "VENDOR" },
                { "modem.bin", "MODEM" },
                { "cache.img", "CACHE" },
                { "cache.img.ext4", "CACHE" },
                { "hidden.img", "HIDDEN" },
                { "hidden.img.ext4", "HIDDEN" },
                { "userdata.img", "USERDATA" },
                { "userdata.img.ext4", "USERDATA" }
            };

        /// <summary>
        /// 解压指定固件包并返回可刷入文件列表。
        /// </summary>
        /// <param name="slot">固件槽位。</param>
        /// <param name="sourceFilePath">固件包绝对路径。</param>
        /// <returns>已解压固件包。</returns>
        public HeimdallFirmwarePackageModel ParsePackage(
            HeimdallFirmwareSlot slot,
            string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("固件文件不存在。", sourceFilePath);
            }

            var package = new HeimdallFirmwarePackageModel
            {
                Slot = slot,
                SourceFilePath = sourceFilePath,
                WorkingDirectory = CreateWorkingDirectory()
            };
            Directory.CreateDirectory(package.WorkingDirectory);

            if (slot == HeimdallFirmwareSlot.TWRP && IsImageFile(sourceFilePath))
            {
                AddDirectRecoveryFile(package, sourceFilePath);
                return package;
            }

            ExtractTarEntries(sourceFilePath, package);
            return package;
        }

        /// <summary>
        /// 使用本地 TWRP 镜像替换包内 recovery 镜像。
        /// </summary>
        /// <param name="package">目标固件包。</param>
        /// <param name="twrpFilePath">TWRP 镜像绝对路径。</param>
        public void ApplyRecoveryOverride(
            HeimdallFirmwarePackageModel package,
            string twrpFilePath)
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
                throw new FileNotFoundException("TWRP 镜像不存在。", twrpFilePath);
            }

            var targetPath = Path.Combine(package.WorkingDirectory, "recovery.img");
            File.Copy(twrpFilePath, targetPath, true);
            var recoveryFile = package.Files.FirstOrDefault(item =>
                string.Equals(item.SuggestedPartitionName, "RECOVERY", StringComparison.OrdinalIgnoreCase));
            if (recoveryFile == null)
            {
                recoveryFile = new HeimdallFirmwareFileModel();
                package.Files.Add(recoveryFile);
            }

            recoveryFile.EntryName = "recovery.img";
            recoveryFile.ExtractedFilePath = targetPath;
            recoveryFile.Size = new FileInfo(targetPath).Length;
            recoveryFile.SuggestedPartitionName = "RECOVERY";
        }

        /// <summary>
        /// 清理程序运行目录下的全部固件解压目录。
        /// </summary>
        public static void CleanupExtractRootDirectory()
        {
            DeleteDirectoryQuietly(GetExtractRootDirectory());
        }

        /// <summary>
        /// 清理本次刷入创建的固件解压目录。
        /// </summary>
        /// <param name="packages">本次解析的固件包。</param>
        public static void CleanupPackageWorkingDirectories(
            IEnumerable<HeimdallFirmwarePackageModel> packages)
        {
            if (packages == null)
            {
                return;
            }

            foreach (var package in packages)
            {
                DeleteDirectoryQuietly(package?.WorkingDirectory);
            }
        }

        /// <summary>
        /// 将直接选择的 TWRP 镜像复制为 recovery.img。
        /// </summary>
        /// <param name="package">目标固件包模型。</param>
        /// <param name="sourceFilePath">TWRP 镜像路径。</param>
        private static void AddDirectRecoveryFile(
            HeimdallFirmwarePackageModel package,
            string sourceFilePath)
        {
            var targetPath = Path.Combine(package.WorkingDirectory, "recovery.img");
            File.Copy(sourceFilePath, targetPath, true);
            package.Files.Add(new HeimdallFirmwareFileModel
            {
                EntryName = "recovery.img",
                ExtractedFilePath = targetPath,
                Size = new FileInfo(targetPath).Length,
                SuggestedPartitionName = "RECOVERY"
            });
        }

        /// <summary>
        /// 逐条读取 TAR 内容，只提取固定 Heimdall 分区对应的文件。
        /// </summary>
        /// <param name="sourceFilePath">TAR 或 TAR.MD5 路径。</param>
        /// <param name="package">目标固件包模型。</param>
        private static void ExtractTarEntries(
            string sourceFilePath,
            HeimdallFirmwarePackageModel package)
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

                    var fileName = Path.GetFileName(entryName);
                    if (IsPitFile(fileName))
                    {
                        ExtractPitFile(stream, size, package, fileName);
                        continue;
                    }

                    var imageName = RemoveLz4Suffix(fileName);
                    var partitionName = GetPartitionName(imageName);
                    if (string.IsNullOrWhiteSpace(partitionName))
                    {
                        SkipTarContent(stream, size);
                        continue;
                    }

                    var compressedPath = Path.Combine(package.WorkingDirectory, fileName);
                    using (var output = File.Create(compressedPath))
                    {
                        CopyExactly(stream, output, size);
                    }
                    SkipPadding(stream, size);

                    var imagePath = compressedPath;
                    if (fileName.EndsWith(".lz4", StringComparison.OrdinalIgnoreCase))
                    {
                        imagePath = DecompressLz4File(compressedPath, package.WorkingDirectory, imageName);
                        File.Delete(compressedPath);
                    }

                    package.Files.Add(new HeimdallFirmwareFileModel
                    {
                        EntryName = imageName,
                        ExtractedFilePath = imagePath,
                        Size = new FileInfo(imagePath).Length,
                        SuggestedPartitionName = partitionName
                    });
                }
            }
        }

        /// <summary>
        /// 将 LZ4 文件解压为原始镜像。
        /// </summary>
        /// <param name="sourceFilePath">LZ4 文件路径。</param>
        /// <param name="workingDirectory">解压目录。</param>
        /// <param name="targetFileName">镜像文件名。</param>
        /// <returns>解压后的镜像路径。</returns>
        private static string DecompressLz4File(
            string sourceFilePath,
            string workingDirectory,
            string targetFileName)
        {
            var targetPath = Path.Combine(workingDirectory, targetFileName);
            using (var input = File.OpenRead(sourceFilePath))
            using (var lz4Stream = LZ4Stream.Decode(input))
            using (var output = File.Create(targetPath))
            {
                lz4Stream.CopyTo(output);
            }

            return targetPath;
        }

        /// <summary>
        /// 根据解压后的文件名获取固定 Heimdall 分区名。
        /// </summary>
        /// <param name="fileName">镜像文件名。</param>
        /// <returns>分区名，未匹配时返回空字符串。</returns>
        private static string GetPartitionName(string fileName)
        {
            return PartitionNameMap.TryGetValue(fileName ?? string.Empty, out var partitionName)
                ? partitionName
                : string.Empty;
        }

        private static bool IsImageFile(string filePath)
        {
            return string.Equals(Path.GetExtension(filePath), ".img", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断 TAR 条目是否为 Odin 固件包内的 PIT 分区表文件。
        /// </summary>
        /// <param name="fileName">固件包内文件名。</param>
        /// <returns>文件扩展名为 .pit 时返回 true。</returns>
        private static bool IsPitFile(string fileName)
        {
            return string.Equals(Path.GetExtension(fileName), ".pit", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 提取固件包内的 PIT 文件，用于 Heimdall 重分区刷入。
        /// </summary>
        /// <param name="stream">TAR 数据流。</param>
        /// <param name="size">PIT 条目大小。</param>
        /// <param name="package">目标固件包模型。</param>
        /// <param name="fileName">PIT 文件名。</param>
        private static void ExtractPitFile(
            Stream stream,
            long size,
            HeimdallFirmwarePackageModel package,
            string fileName)
        {
            var targetPath = Path.Combine(package.WorkingDirectory, fileName);
            using (var output = File.Create(targetPath))
            {
                CopyExactly(stream, output, size);
            }

            SkipPadding(stream, size);
            package.PitFilePath = targetPath;
            HeimdallDebugLogService.Write("Package", "extract-pit Slot=" + package.Slot + " File=" + targetPath);
        }

        private static string RemoveLz4Suffix(string fileName)
        {
            return fileName != null && fileName.EndsWith(".lz4", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - 4)
                : fileName ?? string.Empty;
        }

        private static string CreateWorkingDirectory()
        {
            return Path.Combine(GetExtractRootDirectory(), Guid.NewGuid().ToString("N"));
        }

        private static string GetExtractRootDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtractRootDirectoryName);
        }

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
                HeimdallDebugLogService.Write(
                    "Package",
                    "delete-directory-failed Path=" + directoryPath + " Error=" + ex.Message);
            }
        }

        private static bool IsEmptyHeader(byte[] header)
        {
            return header.All(value => value == 0);
        }

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

            return Encoding.ASCII.GetString(bytes, offset, end - offset);
        }

        private static void CopyExactly(Stream input, Stream output, long bytesToCopy)
        {
            var buffer = new byte[1024 * 1024];
            var remaining = bytesToCopy;
            while (remaining > 0)
            {
                var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
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
            var padding = (512 - size % 512) % 512;
            if (padding > 0)
            {
                stream.Seek(padding, SeekOrigin.Current);
            }
        }
    }
}
