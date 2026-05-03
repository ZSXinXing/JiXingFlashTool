using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using JXAdbCore.Receivers;

namespace JXAdbCore.Interface
{
    public partial interface IAdbClient
    {
        Task<int> GetAdbVersionAsync(CancellationToken cancellationToken);
        Task KillAdbAsync(CancellationToken cancellationToken);

        Task<List<DeviceData>> GetDevicesAsync(CancellationToken cancellationToken);
        Task<int> CreateForwardAsync(DeviceData device, string local, string remote, bool allowRebind, CancellationToken cancellationToken);
        Task<int> CreateForwardAsync(DeviceData device, ForwardSpec local, ForwardSpec remote, bool allowRebind, CancellationToken cancellationToken);
        Task<int> CreateReverseForwardAsync(DeviceData device, string remote, string local, bool allowRebind, CancellationToken cancellationToken);

        Task RemoveReverseForwardAsync(DeviceData device, string remote, CancellationToken cancellationToken);
        Task RemoveAllReverseForwardsAsync(DeviceData device, CancellationToken cancellationToken);
        Task RemoveForwardAsync(DeviceData device, int localPort, CancellationToken cancellationToken);
        Task RemoveAllForwardsAsync(DeviceData device, CancellationToken cancellationToken);
        Task<IEnumerable<ForwardData>> ListForwardAsync(DeviceData device, CancellationToken cancellationToken);
        Task<IEnumerable<ForwardData>> ListReverseForwardAsync(DeviceData device, CancellationToken cancellationToken);
        Task ExecuteRemoteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, CancellationToken cancellationToken);
        Task ExecuteRemoteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, Encoding encoding, CancellationToken cancellationToken);

        Task RebootAsync(string into, DeviceData device, CancellationToken cancellationToken);
        Task<string> PairAsync(DnsEndPoint endpoint, string code, CancellationToken cancellationToken);
        Task<string> ConnectAsync(DnsEndPoint endpoint, CancellationToken cancellationToken);
        Task<string> DisconnectAsync(DnsEndPoint endpoint, CancellationToken cancellationToken);
        Task RootAsync(DeviceData device, CancellationToken cancellationToken);
        Task UnrootAsync(DeviceData device, CancellationToken cancellationToken);
        Task InstallAsync(DeviceData device, Stream apk, params string[] arguments);
        Task InstallAsync(DeviceData device, Stream apk, CancellationToken cancellationToken, params string[] arguments);
        Task InstallMultipleAsync(DeviceData device, Stream[] splitAPKs, string packageName, params string[] arguments);
        Task InstallMultipleAsync(DeviceData device, Stream[] splitAPKs, string packageName, CancellationToken cancellationToken, params string[] arguments);
        Task InstallMultipleAsync(DeviceData device, Stream baseAPK, Stream[] splitAPKs, params string[] arguments);
        Task InstallMultipleAsync(DeviceData device, Stream baseAPK, Stream[] splitAPKs, CancellationToken cancellationToken, params string[] arguments);
        Task<string> InstallCreateAsync(DeviceData device, string packageName, params string[] arguments);
        Task<string> InstallCreateAsync(DeviceData device, string packageName, CancellationToken cancellationToken, params string[] arguments);
        Task InstallWriteAsync(DeviceData device, Stream apk, string apkName, string session, CancellationToken cancellationToken);
        Task InstallCommitAsync(DeviceData device, string session, CancellationToken cancellationToken);
        Task SendKeyEventAsync(DeviceData device, string key, CancellationToken cancellationToken);
        Task SendTextAsync(DeviceData device, string text, CancellationToken cancellationToken);
        Task ClearInputAsync(DeviceData device, int charcount, CancellationToken cancellationToken);
    }
}
