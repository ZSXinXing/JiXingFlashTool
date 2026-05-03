using JXAdbCore.Enums;
using JXAdbCore.EventHandler;
using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class SyncService : ISyncService, IDisposable
    {
        private const int MaxPathLength = 1024;
        public event EventHandler<SyncProgressChangedEventArgs> SyncProgressChanged;
        public SyncService(IAdbClient client, DeviceData device) : this(Factories.AdbSocketFactory(client.EndPoint), device)
        {
        }

        public SyncService(IAdbSocket socket, DeviceData device)
        {
            Socket = socket;
            Device = device;

            Open();
        }

        public int MaxBufferSize { get; set; } = 64 * 1024;

        public DeviceData Device { get; private set; }
        public IAdbSocket Socket { get; private set; }

        public bool IsOpen => Socket != null && Socket.Connected;

        public void Open()
        {
            Socket.SetDevice(Device);

            Socket.SendAdbRequest("sync:");
            Socket.ReadAdbResponse();
        }

        public void Reopen(IAdbSocket socket)
        {
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }
            Socket = socket;
            Open();
        }

        public void Reopen(IAdbClient client) => Reopen(Factories.AdbSocketFactory(client.EndPoint));

        public void Push(Stream stream, string remotePath, int permissions, DateTimeOffset timestamp, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (remotePath == null)
            {
                throw new ArgumentNullException(nameof(remotePath));
            }

            if (remotePath.Length > MaxPathLength)
            {
                throw new ArgumentOutOfRangeException(nameof(remotePath), $"The remote path {remotePath} exceeds the maximum path size {MaxPathLength}");
            }

            Socket.SendSyncRequest(SyncCommand.SEND, remotePath, permissions);

            byte[] buffer = new byte[MaxBufferSize];

            byte[] dataBytes = SyncCommandConverter.GetBytes(SyncCommand.DATA);
            byte[] lengthBytes = BitConverter.GetBytes(MaxBufferSize);
            int headerSize = dataBytes.Length + lengthBytes.Length;
            int reservedHeaderSize = headerSize;
            int maxDataSize = MaxBufferSize - reservedHeaderSize;
            lengthBytes = BitConverter.GetBytes(maxDataSize);

            long totalBytesToProcess = stream.CanSeek ? stream.Length : 0;
            long totalBytesRead = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int read = stream.Read(buffer, headerSize, maxDataSize);
                totalBytesRead += read;

                if (read == 0)
                {
                    break;
                }
                else if (read != maxDataSize)
                {
                    lengthBytes = BitConverter.GetBytes(read);
                    headerSize = dataBytes.Length + lengthBytes.Length;
                }

                int startPosition = reservedHeaderSize - headerSize;

                Buffer.BlockCopy(dataBytes, 0, buffer, startPosition, dataBytes.Length);
                Buffer.BlockCopy(lengthBytes, 0, buffer, startPosition + dataBytes.Length, lengthBytes.Length);
                Socket.Send(buffer, startPosition, read + dataBytes.Length + lengthBytes.Length);

                SyncProgressChanged?.Invoke(this, new SyncProgressChangedEventArgs(totalBytesRead, totalBytesToProcess));
                if (progress != null && totalBytesToProcess != 0)
                {
                    progress.Report((int)(100.0 * totalBytesRead / totalBytesToProcess));
                }
            }

            int time = (int)timestamp.ToUnixTimeSeconds();
            Socket.SendSyncRequest(SyncCommand.DONE, time);

            SyncCommand result = Socket.ReadSyncResponse();

            if (result == SyncCommand.FAIL)
            {
                string message = Socket.ReadSyncString();

                throw new AdbException(message);
            }
            else if (result != SyncCommand.OKAY)
            {
                throw new AdbException($"The server sent an invali repsonse {result}");
            }
        }

        public void Pull(string remoteFilepath, Stream stream, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (remoteFilepath == null)
            {
                throw new ArgumentNullException(nameof(remoteFilepath));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            FileStatistics stat = Stat(remoteFilepath);
            long totalBytesToProcess = stat.Size;
            long totalBytesRead = 0;

            byte[] buffer = new byte[MaxBufferSize];

            Socket.SendSyncRequest(SyncCommand.RECV, remoteFilepath);

            while (true)
            {
                SyncCommand response = Socket.ReadSyncResponse();
                cancellationToken.ThrowIfCancellationRequested();

                if (response == SyncCommand.DONE)
                {
                    break;
                }
                else if (response == SyncCommand.FAIL)
                {
                    string message = Socket.ReadSyncString();
                    throw new AdbException($"Failed to pull '{remoteFilepath}'. {message}");
                }
                else if (response != SyncCommand.DATA)
                {
                    throw new AdbException($"The server sent an invalid response {response}");
                }

                byte[] reply = new byte[4];
                _ = Socket.Read(reply);

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(reply);
                }

                int size = BitConverter.ToInt32(reply, 0);

                if (size > MaxBufferSize)
                {
                    throw new AdbException($"The adb server is sending {size} bytes of data, which exceeds the maximum chunk size {MaxBufferSize}");
                }

                _ = Socket.Read(buffer, size);
                stream.Write(buffer, 0, size);
                totalBytesRead += size;

                SyncProgressChanged?.Invoke(this, new SyncProgressChangedEventArgs(totalBytesRead, totalBytesToProcess));

                if (progress != null && totalBytesToProcess != 0)
                {
                    progress.Report((int)(100.0 * totalBytesRead / totalBytesToProcess));
                }
            }
        }

        public FileStatistics Stat(string remotePath)
        {
            Socket.SendSyncRequest(SyncCommand.STAT, remotePath);

            if (Socket.ReadSyncResponse() != SyncCommand.STAT)
            {
                throw new AdbException($"The server returned an invalid sync response.");
            }

            FileStatistics value = new FileStatistics()
            {
                Path = remotePath
            };

            ReadStatistics(value);

            return value;
        }

        public IEnumerable<FileStatistics> GetDirectoryListing(string remotePath)
        {
            Collection<FileStatistics> value = new Collection<FileStatistics>();

            // create the stat request message.
            Socket.SendSyncRequest(SyncCommand.LIST, remotePath);

            while (true)
            {
                SyncCommand response = Socket.ReadSyncResponse();

                if (response == SyncCommand.DONE)
                {
                    break;
                }
                else if (response != SyncCommand.DENT)
                {
                    throw new AdbException($"The server returned an invalid sync response.");
                }

                FileStatistics entry = new FileStatistics();
                ReadStatistics(entry);
                entry.Path = Socket.ReadSyncString();

                value.Add(entry);
            }

            return value;
        }

        public void Dispose()
        {
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }
        }

        private void ReadStatistics(FileStatistics value)
        {
            byte[] statResult = new byte[12];
            _ = Socket.Read(statResult);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(statResult, 0, 4);
                Array.Reverse(statResult, 4, 4);
                Array.Reverse(statResult, 8, 4);
            }

            value.FileMode = (UnixFileMode)BitConverter.ToInt32(statResult, 0);
            value.Size = BitConverter.ToInt32(statResult, 4);
            value.Time = Utilities.FromUnixTimeSeconds(BitConverter.ToInt32(statResult, 8));
        }
    }
}
