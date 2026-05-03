using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class TcpSocket : ITcpSocket
    {
        private Socket socket;
        private EndPoint endPoint;

        public TcpSocket() => socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public bool Connected => socket.Connected;

        public int ReceiveBufferSize {
            get => socket.ReceiveBufferSize;
            set => socket.ReceiveBufferSize = value;
        }

        public void Connect(EndPoint endPoint)
        {
            try
            {
                socket.Connect(endPoint);
                socket.Blocking = true;
                this.endPoint = endPoint;
            }
            catch(Exception ex)
            {
            }
        }

        public void Reconnect()
        {
            if (socket.Connected)
            {
                return;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Connect(endPoint);
        }

        public Stream GetStream() => new NetworkStream(socket);

        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int size, SocketFlags socketFlags, CancellationToken cancellationToken) =>
            await Utilities.Run(() => Send(buffer, offset, size, socketFlags), cancellationToken);

        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags) => socket.Send(buffer, offset, size, socketFlags);

        public Task<int> SendAsync(byte[] buffer, int offset, int size, SocketFlags socketFlags, CancellationToken cancellationToken) =>
            socket.ReceiveAsync(buffer, offset, size, socketFlags, cancellationToken);

        public int Receive(byte[] buffer, int size, SocketFlags socketFlags) => socket.Receive(buffer, size, socketFlags);

        public void Dispose()
        {
            socket.Close();
            socket.Dispose();
        }
    }
}
