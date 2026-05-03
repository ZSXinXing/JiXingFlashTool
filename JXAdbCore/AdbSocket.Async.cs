using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public partial class AdbSocket
    {
        public Task SendAsync(byte[] data, int length, CancellationToken cancellationToken = default) => SendAsync(data, 0, length, cancellationToken);

        public async Task SendAsync(byte[] data, int offset, int length, CancellationToken cancellationToken = default)
        {
            try
            {
                int count = await socket.SendAsync(data, offset, length != -1 ? length : data.Length, SocketFlags.None, cancellationToken);
                if (count < 0)
                {
                    throw new AdbException("channel EOF");
                }
            }
            catch (SocketException ex)
            {
                throw ex;
            }
        }

        public async Task SendAdbRequestAsync(string request, CancellationToken cancellationToken = default)
        {
            byte[] data = AdbClient.FormAdbRequest(request);

            if (!await WriteAsync(data, cancellationToken))
            {
                throw new IOException($"Failed sending the request '{request}' to ADB");
            }
        }

        public Task ReadAsync(byte[] data, CancellationToken cancellationToken = default) =>
            ReadAsync(data, data.Length, cancellationToken);

        public async Task<int> ReadAsync(byte[] data, int length, CancellationToken cancellationToken = default)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length < length)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            int count = -1;
            int totalRead = 0;

            while (count != 0 && totalRead < length)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    int left = length - totalRead;
                    int buflen = left < ReceiveBufferSize ? left : ReceiveBufferSize;

                    count = await socket.ReceiveAsync(data, totalRead, buflen, SocketFlags.None, cancellationToken).ConfigureAwait(false);

                    if (count < 0)
                    {
                        throw new AdbException("EOF");
                    }
                    else if (count == 0)
                    {
                    }
                    else
                    {
                        totalRead += count;
                    }
                }
                catch (SocketException ex)
                {
                  //  throw new AdbException($"An error occurred while receiving data from the adb server: {ex.Message}.", ex);
                }
            }

            return totalRead;
        }

        public async Task<string> ReadStringAsync(CancellationToken cancellationToken = default)
        {
            byte[] reply = new byte[4];
            await ReadAsync(reply, cancellationToken).ConfigureAwait(false);

            string lenHex = AdbClient.Encoding.GetString(reply);
            int len = int.Parse(lenHex, NumberStyles.HexNumber);

            reply = new byte[len];
            await ReadAsync(reply, cancellationToken).ConfigureAwait(false);

            string value = AdbClient.Encoding.GetString(reply);
            return value;
        }

        public async Task<AdbResponse> ReadAdbResponseAsync(CancellationToken cancellationToken = default)
        {
            AdbResponse response = await ReadAdbResponseInnerAsync(cancellationToken);

            if (!response.IOSuccess || !response.Okay)
            {
                socket.Dispose();
               // throw new AdbException($"An error occurred while reading a response from ADB: {response.Message}", response);
            }

            return response;
        }

        public async Task SetDeviceAsync(DeviceData device, CancellationToken cancellationToken = default)
        {
            if (device != null)
            {
                await SendAdbRequestAsync($"host:transport:{device.Serial}", cancellationToken);

                try
                {
                    AdbResponse response = await ReadAdbResponseAsync(cancellationToken);
                }
                catch (AdbException e)
                {
                }
            }
        }

        protected async Task<bool> WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            try
            {
                await SendAsync(data, -1, cancellationToken);
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

        protected async Task<AdbResponse> ReadAdbResponseInnerAsync(CancellationToken cancellationToken = default)
        {
            AdbResponse resp = new AdbResponse();

            byte[] reply = new byte[4];
            await ReadAsync(reply);

            resp.IOSuccess = true;

            resp.Okay = IsOkay(reply);

            if (!resp.Okay)
            {
                string message = await ReadStringAsync(cancellationToken);
                resp.Message = message;
            }

            return resp;
        }
    }
}
