using System;
using System.IO;
using System.Text;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Odin 协议二进制读写扩展，负责小端整数、固定长度字符串和响应错误码处理。
    /// </summary>
    internal static class OdinBinaryExtensions
    {
        /// <summary>
        /// 将缓冲区扩展到 Odin 命令要求的 1024 字节对齐长度。
        /// </summary>
        /// <param name="buffer">原始缓冲区。</param>
        /// <returns>对齐后的缓冲区。</returns>
        public static byte[] AlignOdinCommand(this byte[] buffer)
        {
            Array.Resize(ref buffer, 1024);
            return buffer;
        }

        /// <summary>
        /// 写入 32 位整数到指定偏移。
        /// </summary>
        /// <param name="buffer">目标缓冲区。</param>
        /// <param name="value">整数值。</param>
        /// <param name="offset">写入偏移。</param>
        public static void WriteInt32LittleEndian(this byte[] buffer, int value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
        }

        /// <summary>
        /// 写入 64 位整数到指定偏移。
        /// </summary>
        /// <param name="buffer">目标缓冲区。</param>
        /// <param name="value">整数值。</param>
        /// <param name="offset">写入偏移。</param>
        public static void WriteInt64LittleEndian(this byte[] buffer, long value, int offset)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            Buffer.BlockCopy(bytes, 0, buffer, offset, 8);
        }

        /// <summary>
        /// 从指定偏移读取 32 位整数。
        /// </summary>
        /// <param name="buffer">源缓冲区。</param>
        /// <param name="offset">读取偏移。</param>
        /// <returns>解析后的整数值。</returns>
        public static int ReadInt32LittleEndian(this byte[] buffer, int offset)
        {
            var bytes = new byte[4];
            Buffer.BlockCopy(buffer, offset, bytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// 读取固定长度 ASCII 字符串并移除尾部空字符。
        /// </summary>
        /// <param name="reader">二进制读取器。</param>
        /// <param name="count">固定读取长度。</param>
        /// <returns>字符串内容。</returns>
        public static string ReadFixedAsciiString(this BinaryReader reader, int count)
        {
            return Encoding.ASCII.GetString(reader.ReadBytes(count)).TrimEnd('\0');
        }

        /// <summary>
        /// 检查 Odin 响应是否为失败响应，并转换为可定位的异常。
        /// </summary>
        /// <param name="buffer">Odin 响应缓冲区。</param>
        /// <param name="stage">当前协议阶段。</param>
        /// <param name="isFlashEnd">是否为刷入序列结束阶段。</param>
        public static void ThrowIfOdinFailed(this byte[] buffer, string stage, bool isFlashEnd = false)
        {
            if (buffer == null || buffer.Length == 0 || buffer[0] != 0xFF)
            {
                return;
            }

            var error = buffer.Length >= 8 ? buffer.ReadInt32LittleEndian(4) : 0;
            var message = stage + " received Odin failure code 0x" + error.ToString("X4");
            if (isFlashEnd)
            {
                message = error switch
                {
                    -7 => message + " (Ext4)",
                    -6 => message + " (Size)",
                    -5 => message + " (Auth)",
                    -4 => message + " (Write)",
                    -3 => message + " (Erase)",
                    -2 => message + " (WriteProtect)",
                    _ => message
                };
            }

            throw new InvalidDataException(message);
        }
    }
}
