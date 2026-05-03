using JXAdbCore.Enums;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public partial interface IAdbSocket : IDisposable
    {
        bool Connected { get; }
        void Reconnect();
        void Send(byte[] data, int length);
        void Send(byte[] data, int offset, int length);
        void SendSyncRequest(SyncCommand command, string path, int permissions);
        void SendSyncRequest(SyncCommand command, string path);
        void SendSyncRequest(SyncCommand command, int length);
        void SendAdbRequest(string request);
        int Read(byte[] data);
        int Read(byte[] data, int length);
        string ReadString();
        string ReadSyncString();
        SyncCommand ReadSyncResponse();
        AdbResponse ReadAdbResponse();
        Stream GetShellStream();
        void SetDevice(DeviceData device);
    }
}
