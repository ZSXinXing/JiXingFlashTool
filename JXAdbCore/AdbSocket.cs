using JXAdbCore.Enums;
using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public partial class AdbSocket : IAdbSocket,IDisposable
    {
        private readonly ITcpSocket socket;
        public static int ReceiveBufferSize { get; set; } = 40960;

        public static bool IsOkay(byte[] reply) => AdbClient.Encoding.GetString(reply).Equals("OKAY");
        public AdbSocket(EndPoint endPoint)
        {
            socket = new TcpSocket();
            socket.Connect(endPoint);
        }

        public bool Connected => socket.Connected;

        public Stream GetShellStream()
        {
            Stream stream = socket.GetStream();
            return stream;
        }

        public int Read(byte[] data) => Read(data, data.Length);

        public int Read(byte[] data, int length)
        {
            int expLen = length != -1 ? length : data.Length;
            int count = -1;
            int totalRead = 0;

            while (count != 0 && totalRead < expLen)
            {
                try
                {
                    int left = expLen - totalRead;
                    int buflen = left < ReceiveBufferSize ? left : ReceiveBufferSize;

                    byte[] buffer = new byte[buflen];
                    count = socket.Receive(buffer, buflen, SocketFlags.None);
                    if (count < 0)
                    {
                        throw new AdbException("EOF");
                    }
                    else if (count == 0)
                    {
                    }
                    else
                    {
                        Array.Copy(buffer, 0, data, totalRead, count);
                        totalRead += count;
                    }
                }
                catch (SocketException sex)
                {
                    break;
                }
            }

            return totalRead;
        }

        protected AdbResponse ReadAdbResponseInner()
        {
            AdbResponse resp = new AdbResponse();

            byte[] reply = new byte[4];
            Read(reply);

            resp.IOSuccess = true;

            resp.Okay = IsOkay(reply);

            if (!resp.Okay)
            {
                string message = ReadString();
                resp.Message = message;
            }

            return resp;
        }

        public AdbResponse ReadAdbResponse()
        {
            AdbResponse response = ReadAdbResponseInner();

            if (!response.IOSuccess || !response.Okay)
            {
                //socket.Dispose();
                //throw new AdbException($"An error occurred while reading a response from ADB: {response.Message}", response);
            }

            return response;
        }

        public virtual string ReadSyncString()
        {
            byte[] reply = new byte[4];
            Read(reply);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(reply);
            }

            int len = BitConverter.ToInt32(reply, 0);

            reply = new byte[len];
            Read(reply);

            string value = AdbClient.Encoding.GetString(reply);
            return value;
        }

        public virtual SyncCommand ReadSyncResponse()
        {
            byte[] data = new byte[4];
            Read(data);

            return SyncCommandConverter.GetCommand(data);
        }

        public string ReadString()
        {
            byte[] reply = new byte[4];
            int read = Read(reply);

            if (read == 0)
            {
                return null;
            }
            string lenHex = AdbClient.Encoding.GetString(reply);
            int len = int.Parse(lenHex, NumberStyles.HexNumber);

            reply = new byte[len];
            Read(reply);

            string value = AdbClient.Encoding.GetString(reply);
            return value;
        }


        public void Reconnect()
        {
        }

        protected bool Write(byte[] data)
        {
            try
            {
                Send(data, data.Length);
            }
            catch(Exception ex) {
            }

            return true;
        }

        public void Send(byte[] data, int length) => Send(data, 0, length);

        public void Send(byte[] data, int offset, int length)
        {
            try
            {
                int count = socket.Send(data, 0, length != -1 ? length : data.Length, SocketFlags.None);
            }
            catch (SocketException sex)
            {
            }
        }


        public void SendSyncRequest(SyncCommand command, string path, int permissions) =>
            SendSyncRequest(command, $"{path},{permissions}");

        public void SendSyncRequest(SyncCommand command, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            byte[] pathBytes = AdbClient.Encoding.GetBytes(path);
            SendSyncRequest(command, pathBytes.Length);
            Write(pathBytes);
        }

        public void SendSyncRequest(SyncCommand command, int length)
        {
            byte[] commandBytes = SyncCommandConverter.GetBytes(command);

            byte[] lengthBytes = BitConverter.GetBytes(length);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            Write(commandBytes);
            Write(lengthBytes);
        }

        public void SendAdbRequest(string request)
        {
            byte[] data = AdbClient.FormAdbRequest(request);

            Write(data);
        }

        public void SetDevice(DeviceData device)
        {
            if (device != null)
            {
                if (socket.Connected == false) return;

                SendAdbRequest($"host:transport:{device.Serial}");

                try
                {
                    AdbResponse response = ReadAdbResponse();
                }
                catch (Exception ex) { 
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
