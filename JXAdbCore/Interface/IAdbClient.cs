using JXAdbCore.EventHandler;
using JXAdbCore.Model;
using JXAdbCore.Receivers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public partial interface IAdbClient
    {
        EndPoint EndPoint { get; }
        int GetAdbVersion();
        void KillAdb();
        List<DeviceData> GetDevices();

        int CreateForward(DeviceData device, string local, string remote, bool allowRebind);
        int CreateReverseForward(DeviceData device, string remote, string local, bool allowRebind);
        void RemoveReverseForward(DeviceData device, string remote);
        void RemoveAllReverseForwards(DeviceData device);
        void RemoveForward(DeviceData device, int localPort);
        void RemoveAllForwards(DeviceData device);
        void ExecuteRemoteCommand(string command, DeviceData device, IShellOutputReceiver receiver);
        void ExecuteRemoteCommand(string command, DeviceData device, IShellOutputReceiver receiver, Encoding encoding);
        void Reboot(string into, DeviceData device);
        bool Connect(DnsEndPoint endpoint);
        bool Disconnect(DnsEndPoint endpoint);
        void Root(DeviceData device);
        void Unroot(DeviceData device);
        void Install(DeviceData device, Stream apk, params string[] arguments);
        void InstallMultiple(DeviceData device, Stream[] splitAPKs, string packageName, params string[] arguments);
        void InstallMultiple(DeviceData device, Stream baseAPK, Stream[] splitAPKs, params string[] arguments);
        string InstallCreate(DeviceData device, string packageName = null, params string[] arguments);
        void InstallWrite(DeviceData device, Stream apk, string apkName, string session);
        void InstallCommit(DeviceData device, string session);
        void Pull(DeviceData device, string remotePath, Stream stream, EventHandler<SyncProgressChangedEventArgs> syncProgressEventHandler = null, IProgress<int> progress = null, CancellationToken cancellationToken = default);
        void Push(DeviceData device,string remotePath, Stream stream, int permissions, DateTimeOffset timestamp,EventHandler<SyncProgressChangedEventArgs> syncProgressEventHandler = null,IProgress<int> progress = null, CancellationToken cancellationToken = default);

    }
}
