using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public interface ITcpSocket : IDisposable
    {
        bool Connected { get; }
        int ReceiveBufferSize { get; set; }
        void Connect(EndPoint endPoint);
        void Reconnect();
        int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags);
        public Task<int> SendAsync(byte[] buffer, int offset, int size, SocketFlags socketFlags, CancellationToken cancellationToken);
        int Receive(byte[] buffer, int size, SocketFlags socketFlags);
        Task<int> ReceiveAsync(byte[] buffer, int offset, int size, SocketFlags socketFlags, CancellationToken cancellationToken);
        Stream GetStream();
    }
}
