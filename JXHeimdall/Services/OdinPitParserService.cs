using JXHeimdall.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Samsung PIT 二进制分区表解析服务，用于 Odin 串口刷入流程匹配分区。
    /// </summary>
    public sealed class OdinPitParserService
    {
        /// <summary>
        /// 从 PIT 二进制内容解析 Odin 原始分区条目。
        /// </summary>
        /// <param name="content">PIT 文件二进制内容。</param>
        /// <returns>解析出的分区条目集合。</returns>
        public IReadOnlyList<OdinPitEntryModel> Parse(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                throw new InvalidDataException("PIT 内容为空，无法解析分区表。");
            }

            using (var stream = new MemoryStream(content))
            using (var reader = new BinaryReader(stream))
            {
                var magic = reader.ReadInt32();
                if (magic != 0x12349876)
                {
                    throw new InvalidDataException("PIT 魔数不匹配，无法解析分区表。");
                }

                var entryCount = reader.ReadInt32();
                reader.ReadFixedAsciiString(8);
                reader.ReadFixedAsciiString(8);
                reader.ReadInt32();

                var entries = new List<OdinPitEntryModel>();
                for (var index = 0; index < entryCount; index++)
                {
                    entries.Add(new OdinPitEntryModel
                    {
                        BinaryType = reader.ReadInt32(),
                        DeviceType = reader.ReadInt32(),
                        PartitionId = reader.ReadInt32(),
                        Attributes = reader.ReadInt32(),
                        UpdateAttributes = reader.ReadInt32(),
                        BlockSize = reader.ReadInt32(),
                        BlockCount = reader.ReadInt32(),
                        FileOffset = reader.ReadInt32(),
                        FileSize = reader.ReadInt32(),
                        PartitionName = reader.ReadFixedAsciiString(32),
                        FileName = reader.ReadFixedAsciiString(32),
                        DeltaName = reader.ReadFixedAsciiString(32)
                    });
                }

                return entries;
            }
        }
    }
}
