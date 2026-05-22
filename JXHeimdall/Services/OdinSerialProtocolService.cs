using JXHeimdall.Models;
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace JXHeimdall.Services
{
    /// <summary>
    /// Windows 串口 Odin 协议服务，参考 Thor 的 Odin 协议实现执行握手、PIT 读取和分区刷入。
    /// </summary>
    public sealed class OdinSerialProtocolService : IDisposable
    {
        private static readonly OdinSerialHandshakeOption[] HandshakeOptions =
        {
            new OdinSerialHandshakeOption(false, false),
            new OdinSerialHandshakeOption(true, true),
            new OdinSerialHandshakeOption(true, false),
            new OdinSerialHandshakeOption(false, true)
        };

        private readonly SerialPort _serialPort;
        private int _flashTimeout = 30000;
        private int _flashPacketSize = 131072;
        private int _flashSequence = 240;

        /// <summary>
        /// 初始化 Odin 串口协议服务。
        /// </summary>
        /// <param name="portName">Windows COM 口名称。</param>
        public OdinSerialProtocolService(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Odin 串口名称不能为空。", nameof(portName));
            }

            _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 120000,
                WriteTimeout = 120000,
                ReadBufferSize = 1024 * 1024,
                WriteBufferSize = 1024 * 1024,
                ReceivedBytesThreshold = 1
            };
        }

        /// <summary>
        /// 打开串口并执行 Odin 握手。
        /// </summary>
        public void OpenAndHandshake()
        {
            foreach (var option in HandshakeOptions)
            {
                ClosePortQuietly();
                ConfigureHandshakeOption(option);
                HeimdallDebugLogService.Write("OdinSerial", "Handshake open dtr=" + option.DtrEnable + " rts=" + option.RtsEnable);
                if (!TryOpenPort(option))
                {
                    continue;
                }

                Thread.Sleep(250);

                if (TryReadHandshakeResponse("Handshake/Stale", 250, out var staleText))
                {
                    HeimdallDebugLogService.Write("OdinSerial", "Handshake stale response=" + staleText);
                    return;
                }

                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                if (TryHandshakeWithCurrentPort(option))
                {
                    return;
                }
            }

            throw new TimeoutException("Odin 串口握手失败：已尝试多组 DTR/RTS 控制线并多次发送 ODIN，但设备未返回 LOKE。请完全退出并重新进入 Download 模式后重试。");
        }

        /// <summary>
        /// 尝试打开当前 Download 串口，打开失败时记录系统错误并允许尝试下一组控制线。
        /// </summary>
        /// <param name="option">当前串口控制线配置。</param>
        /// <returns>串口打开成功时返回 true。</returns>
        private bool TryOpenPort(OdinSerialHandshakeOption option)
        {
            try
            {
                _serialPort.Open();
                return true;
            }
            catch (IOException ex)
            {
                HeimdallDebugLogService.Write("OdinSerial", "Handshake open failed dtr=" + option.DtrEnable + " rts=" + option.RtsEnable + " error=" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 使用当前已打开的串口配置执行 Odin 握手。
        /// </summary>
        /// <param name="option">当前串口控制线配置。</param>
        /// <returns>握手成功时返回 true。</returns>
        private bool TryHandshakeWithCurrentPort(OdinSerialHandshakeOption option)
        {
            var handshakePayloads = new[]
            {
                Encoding.ASCII.GetBytes("ODIN")
            };
            for (var payloadIndex = 0; payloadIndex < handshakePayloads.Length; payloadIndex++)
            {
                for (var retryIndex = 0; retryIndex < 3; retryIndex++)
                {
                    var stage = "Handshake/Dtr" + option.DtrEnable + "/Rts" + option.RtsEnable + "/Payload" + payloadIndex + "/Retry" + retryIndex;
                    HeimdallDebugLogService.Write("OdinSerial", stage + " write length=" + handshakePayloads[payloadIndex].Length);
                    try
                    {
                        WriteBytes(handshakePayloads[payloadIndex], 5000, CancellationToken.None);
                        _serialPort.BaseStream.Flush();
                    }
                    catch (IOException ex)
                    {
                        HeimdallDebugLogService.Write("OdinSerial", stage + " write-failed " + ex.Message);
                        return false;
                    }

                    if (TryReadHandshakeResponse(stage, 1500, out var responseText))
                    {
                        HeimdallDebugLogService.Write("OdinSerial", stage + " response=" + responseText);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 开始 Odin 刷机会话并协商刷入分包大小。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public void BeginSession(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x64, 0);
            buffer.WriteInt32LittleEndian(0x00, 4);
            buffer.WriteInt32LittleEndian(int.MaxValue, 8);
            HeimdallDebugLogService.Write("OdinSerial", "BeginSession request");
            WriteBytes(buffer, 5000, cancellationToken);

            var response = ReadExactly(8, 5000, cancellationToken, "BeginSession");
            LogResponse("BeginSession", response);
            response.ThrowIfOdinFailed("BeginSession");
            var version = BitConverter.ToInt16(new[] { response[6], response[7] }, 0);
            HeimdallDebugLogService.Write("OdinSerial", "BeginSession version=" + version);
            if (version >= 2)
            {
                _flashTimeout = 120000;
                // Windows usbser 后端发送 0x64/0x05 后，部分设备会在下一条 Odin 命令不再响应。
                // 保持 128KiB 分包、240 包序列，仍然等价于 Thor 的 30MiB 序列窗口。
                _flashPacketSize = 131072;
                _flashSequence = 240;
            }
            else
            {
                _flashTimeout = 30000;
                _flashPacketSize = 131072;
                _flashSequence = 240;
            }
        }

        /// <summary>
        /// 设置本次刷入总字节数。
        /// </summary>
        /// <param name="totalBytes">总字节数。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void SetTotalBytes(long totalBytes, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x64, 0);
            buffer.WriteInt32LittleEndian(0x02, 4);
            buffer.WriteInt64LittleEndian(totalBytes, 8);
            HeimdallDebugLogService.Write("OdinSerial", "SetTotalBytes total=" + totalBytes);
            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, 5000, cancellationToken, "SetTotalBytes");
            LogResponse("SetTotalBytes", response);
            response.ThrowIfOdinFailed("SetTotalBytes");
        }

        /// <summary>
        /// 从设备读取 PIT 分区表二进制内容。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>PIT 二进制内容。</returns>
        public byte[] DumpPit(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x65, 0);
            buffer.WriteInt32LittleEndian(0x01, 4);
            HeimdallDebugLogService.Write("OdinSerial", "RequestPitDump");
            WriteBytes(buffer, 5000, cancellationToken);

            var response = ReadExactly(8, 5000, cancellationToken, "RequestPitDump");
            LogResponse("RequestPitDump", response);
            response.ThrowIfOdinFailed("RequestPitDump");
            var size = response.ReadInt32LittleEndian(4);
            var blocks = (int)Math.Ceiling(size / 500d);
            HeimdallDebugLogService.Write("OdinSerial", "Pit size=" + size + " blocks=" + blocks);
            var pit = new byte[size];
            for (var index = 0; index < blocks; index++)
            {
                buffer = new byte[1024];
                buffer.WriteInt32LittleEndian(0x65, 0);
                buffer.WriteInt32LittleEndian(0x02, 4);
                buffer.WriteInt32LittleEndian(index, 8);
                HeimdallDebugLogService.Write("OdinSerial", "ReadPitBlock index=" + index);
                WriteBytes(buffer, 5000, cancellationToken);

                var copySize = Math.Min(500, size - index * 500);
                var block = ReadExactly(copySize, 5000, cancellationToken, "ReadPitBlock/" + index);
                Buffer.BlockCopy(block, 0, pit, index * 500, copySize);
            }

            TryReadZeroLengthPacket();

            buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x65, 0);
            buffer.WriteInt32LittleEndian(0x03, 4);
            HeimdallDebugLogService.Write("OdinSerial", "EndPitDump");
            WriteBytes(buffer, 5000, cancellationToken);
            response = ReadExactly(8, 5000, cancellationToken, "EndPitDump");
            LogResponse("EndPitDump", response);
            response.ThrowIfOdinFailed("EndPitDump");
            return pit;
        }

        /// <summary>
        /// 刷入单个分区镜像。
        /// </summary>
        /// <param name="stream">待刷入镜像流。</param>
        /// <param name="entry">PIT 分区条目。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void FlashPartition(Stream stream, OdinPitEntryModel entry, Action<long, long, string> progress, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            var length = stream.Length;
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x66, 0);
            buffer.WriteInt32LittleEndian(0x00, 4);
            HeimdallDebugLogService.Write("OdinSerial", "RequestFileFlash partition=" + entry.PartitionName);
            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, 5000, cancellationToken, "RequestFileFlash/" + entry.PartitionName);
            LogResponse("RequestFileFlash/" + entry.PartitionName, response);
            response.ThrowIfOdinFailed("RequestFileFlash");

            var sentBytes = 0L;
            var sequenceSize = _flashPacketSize * _flashSequence;
            var sequenceCount = (int)(length / sequenceSize);
            var lastSequenceSize = (int)(length % sequenceSize);
            if (lastSequenceSize != 0)
            {
                sequenceCount++;
            }
            else
            {
                lastSequenceSize = sequenceSize;
            }

            for (var sequenceIndex = 0; sequenceIndex < sequenceCount; sequenceIndex++)
            {
                var isLast = sequenceIndex + 1 == sequenceCount;
                var realSize = isLast ? lastSequenceSize : sequenceSize;
                var alignedSize = realSize;
                if (realSize % _flashPacketSize != 0)
                {
                    alignedSize += _flashPacketSize - realSize % _flashPacketSize;
                }

                progress?.Invoke(sentBytes, length, "发送 " + entry.PartitionName);
                buffer = new byte[1024];
                buffer.WriteInt32LittleEndian(0x66, 0);
                buffer.WriteInt32LittleEndian(0x02, 4);
                buffer.WriteInt32LittleEndian(alignedSize, 8);
                HeimdallDebugLogService.Write("OdinSerial", "RequestSequenceFlash partition=" + entry.PartitionName + " sequence=" + sequenceIndex + " size=" + alignedSize);
                WriteBytes(buffer, 5000, cancellationToken);
                response = ReadExactly(8, 5000, cancellationToken, "RequestSequenceFlash/" + entry.PartitionName + "/" + sequenceIndex);
                LogResponse("RequestSequenceFlash/" + entry.PartitionName + "/" + sequenceIndex, response);
                response.ThrowIfOdinFailed("RequestSequenceFlash/" + sequenceIndex);

                var parts = alignedSize / _flashPacketSize;
                for (var partIndex = 0; partIndex < parts; partIndex++)
                {
                    var partBuffer = new byte[_flashPacketSize];
                    var read = stream.Read(partBuffer, 0, partBuffer.Length);
                    if (read < partBuffer.Length)
                    {
                        Array.Clear(partBuffer, read, partBuffer.Length - read);
                    }

                    WriteBytes(partBuffer, 120000, cancellationToken);
                    response = ReadExactly(8, 120000, cancellationToken, "SendFilePart/" + entry.PartitionName + "/" + sequenceIndex + "/" + partIndex);
                    LogResponse("SendFilePart/" + entry.PartitionName + "/" + sequenceIndex + "/" + partIndex, response);
                    response.ThrowIfOdinFailed("SendFilePart/" + sequenceIndex);
                    var devicePartIndex = response.ReadInt32LittleEndian(4);
                    if (devicePartIndex != partIndex)
                    {
                        throw new InvalidOperationException("Odin 分包序号不一致，期望 " + partIndex + "，设备返回 " + devicePartIndex + "。");
                    }

                    sentBytes = Math.Min(sentBytes + _flashPacketSize, length);
                    progress?.Invoke(sentBytes, length, "发送 " + entry.PartitionName);
                }

                progress?.Invoke(sentBytes, length, "写入 " + entry.PartitionName);
                EndSequenceFlash(entry, realSize, isLast, cancellationToken);
            }
        }

        /// <summary>
        /// 结束 Odin 会话。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public void EndSession(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x67, 0);
            buffer.WriteInt32LittleEndian(0x00, 4);
            HeimdallDebugLogService.Write("OdinSerial", "EndSession");
            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, 5000, cancellationToken, "EndSession");
            LogResponse("EndSession", response);
            response.ThrowIfOdinFailed("EndSession");
        }

        /// <summary>
        /// 请求设备正常重启。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public void Reboot(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x67, 0);
            buffer.WriteInt32LittleEndian(0x01, 4);
            HeimdallDebugLogService.Write("OdinSerial", "Reboot");
            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, 5000, cancellationToken, "Reboot");
            LogResponse("Reboot", response);
            response.ThrowIfOdinFailed("Reboot");
        }

        /// <summary>
        /// 尝试请求设备重启进入 Recovery，部分 Samsung Bootloader 可能不支持该 Odin 子命令。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public void RebootToRecovery(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x67, 0);
            buffer.WriteInt32LittleEndian(0x04, 4);
            HeimdallDebugLogService.Write("OdinSerial", "RebootToRecovery");
            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, 5000, cancellationToken, "RebootToRecovery");
            LogResponse("RebootToRecovery", response);
            response.ThrowIfOdinFailed("RebootToRecovery");
        }

        /// <summary>
        /// 释放串口资源。
        /// </summary>
        public void Dispose()
        {
            ClosePortQuietly();
            _serialPort.Dispose();
        }

        /// <summary>
        /// 应用当前 Odin 握手需要尝试的串口控制线配置。
        /// </summary>
        /// <param name="option">DTR/RTS 控制线配置。</param>
        private void ConfigureHandshakeOption(OdinSerialHandshakeOption option)
        {
            _serialPort.DtrEnable = option.DtrEnable;
            _serialPort.RtsEnable = option.RtsEnable;
        }

        /// <summary>
        /// 静默关闭串口，便于用不同控制线配置重新打开同一个 Download 设备。
        /// </summary>
        private void ClosePortQuietly()
        {
            if (!_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                _serialPort.Close();
            }
            catch (Exception ex)
            {
                HeimdallDebugLogService.Write("OdinSerial", "Close port ignored " + ex.Message);
            }
        }

        /// <summary>
        /// 发送新协议分包大小。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <summary>
        /// 结束当前刷入序列并等待设备写入结果。
        /// </summary>
        /// <param name="entry">PIT 分区条目。</param>
        /// <param name="realSize">当前序列真实数据大小。</param>
        /// <param name="isLast">是否为最后一个序列。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        private void EndSequenceFlash(OdinPitEntryModel entry, int realSize, bool isLast, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            buffer.WriteInt32LittleEndian(0x66, 0);
            buffer.WriteInt32LittleEndian(0x03, 4);
            if (entry.BinaryType == 1)
            {
                buffer.WriteInt32LittleEndian(0x01, 8);
                buffer.WriteInt32LittleEndian(realSize, 12);
                buffer.WriteInt32LittleEndian(entry.BinaryType, 16);
                buffer.WriteInt32LittleEndian(entry.DeviceType, 20);
                buffer.WriteInt32LittleEndian(isLast ? 1 : 0, 24);
            }
            else
            {
                buffer.WriteInt32LittleEndian(0x00, 8);
                buffer.WriteInt32LittleEndian(realSize, 12);
                buffer.WriteInt32LittleEndian(entry.BinaryType, 16);
                buffer.WriteInt32LittleEndian(entry.DeviceType, 20);
                buffer.WriteInt32LittleEndian(entry.PartitionId, 24);
                buffer.WriteInt32LittleEndian(isLast ? 1 : 0, 28);
                buffer.WriteInt32LittleEndian(0, 32);
                buffer.WriteInt32LittleEndian(0, 36);
            }

            WriteBytes(buffer, 5000, cancellationToken);
            var response = ReadExactly(8, _flashTimeout, cancellationToken, "EndSequenceFlash/" + entry.PartitionName);
            LogResponse("EndSequenceFlash/" + entry.PartitionName, response);
            response.ThrowIfOdinFailed("EndSequenceFlash", true);
        }

        /// <summary>
        /// 记录 Odin 协议响应内容，便于定位设备是否返回 ACK 或错误码。
        /// </summary>
        /// <param name="stage">当前协议阶段。</param>
        /// <param name="response">设备响应数据。</param>
        private static void LogResponse(string stage, byte[] response)
        {
            HeimdallDebugLogService.Write("OdinSerial", stage + " response=" + BitConverter.ToString(response ?? Array.Empty<byte>()));
        }

        /// <summary>
        /// 写入串口数据。
        /// </summary>
        /// <param name="buffer">待写入数据。</param>
        /// <param name="timeout">写入超时时间。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        private void WriteBytes(byte[] buffer, int timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serialPort.WriteTimeout = timeout;
            _serialPort.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 从串口读取指定字节数。
        /// </summary>
        /// <param name="count">需要读取的字节数。</param>
        /// <param name="timeout">读取超时时间。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>读取到的字节。</returns>
        private byte[] ReadExactly(int count, int timeout, CancellationToken cancellationToken, string stage)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _serialPort.ReadTimeout = timeout;
            var buffer = new byte[count];
            var offset = 0;
            try
            {
                while (offset < count)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    offset += _serialPort.Read(buffer, offset, count - offset);
                }

                return buffer;
            }
            catch (TimeoutException ex)
            {
                HeimdallDebugLogService.Write("OdinSerial", "Timeout stage=" + stage + " expected=" + count + " read=" + offset);
                throw new TimeoutException("Odin 串口协议在 " + stage + " 阶段读取超时，期望 " + count + " 字节，实际读取 " + offset + " 字节。请重新进入 Download 模式后重试。", ex);
            }
        }

        /// <summary>
        /// 尝试读取 Odin 握手响应，超时返回 false 以便上层重试。
        /// </summary>
        /// <param name="stage">当前握手阶段。</param>
        /// <param name="responseText">读取到的响应文本。</param>
        /// <returns>读取到 LOKE 时返回 true。</returns>
        private bool TryReadHandshakeResponse(string stage, int timeout, out string responseText)
        {
            responseText = string.Empty;
            try
            {
                var response = ReadExactly(4, timeout, CancellationToken.None, stage);
                responseText = Encoding.ASCII.GetString(response);
                if (string.Equals(responseText, "LOKE", StringComparison.Ordinal))
                {
                    return true;
                }

                HeimdallDebugLogService.Write("OdinSerial", stage + " unexpected-response=" + responseText);
                return false;
            }
            catch (TimeoutException)
            {
                HeimdallDebugLogService.Write("OdinSerial", stage + " no-response");
                return false;
            }
        }

        /// <summary>
        /// 尝试读取零长度包对应的残留数据，失败不影响主流程。
        /// </summary>
        /// <summary>
        /// 表示一次 Odin 串口握手要尝试的控制线状态。
        /// </summary>
        private sealed class OdinSerialHandshakeOption
        {
            /// <summary>
            /// 初始化串口控制线状态。
            /// </summary>
            /// <param name="dtrEnable">是否启用 DTR。</param>
            /// <param name="rtsEnable">是否启用 RTS。</param>
            public OdinSerialHandshakeOption(bool dtrEnable, bool rtsEnable)
            {
                DtrEnable = dtrEnable;
                RtsEnable = rtsEnable;
            }

            /// <summary>
            /// 是否启用 DTR 控制线。
            /// </summary>
            public bool DtrEnable { get; }

            /// <summary>
            /// 是否启用 RTS 控制线。
            /// </summary>
            public bool RtsEnable { get; }
        }

        private void TryReadZeroLengthPacket()
        {
            try
            {
                _serialPort.ReadTimeout = 100;
                var buffer = new byte[1];
                _serialPort.Read(buffer, 0, 0);
            }
            catch
            {
                // Odin 设备并不总是返回可见的 ZLP，忽略该兼容性差异。
            }
        }
    }
}
