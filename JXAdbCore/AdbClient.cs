using JXAdbCore.Enums;
using JXAdbCore.EventHandler;
using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using JXAdbCore.Model;
using JXAdbCore.Receivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public partial class AdbClient : IAdbClient
    {
        public const int AdbServerPort = 5037;
        public const int DefaultPort = 5555;
        private readonly Func<EndPoint, IAdbSocket> adbSocketFactory;

        public static Encoding Encoding { get; set; } = Encoding.UTF8;

        public EndPoint EndPoint { get; private set; }

        public AdbClient() : this(new IPEndPoint(IPAddress.Loopback, AdbServerPort), Factories.AdbSocketFactory)
        {
        }

        public AdbClient(EndPoint endPoint, Func<EndPoint, IAdbSocket> adbSocketFactory)
        {
            EndPoint = endPoint;
            this.adbSocketFactory = adbSocketFactory;
        }

        public void KillServer()
        {
        }

        public int GetServerVersion()
        {
            return 0;
        }


        public List<DeviceData> GetDevices()
        {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SendAdbRequest("host:devices-l");
            socket.ReadAdbResponse();
            string reply = socket.ReadString();

            string[] data = reply.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            return data.Select(DeviceData.CreateFromAdbData).ToList();
        }

        public static byte[] FormAdbRequest(string req)
        {
            int payloadLength = Encoding.GetByteCount(req);
            string resultStr = string.Format("{0}{1}", payloadLength.ToString("X4"), req);
            byte[] result = Encoding.GetBytes(resultStr);
            return result;
        }

        public static byte[] CreateAdbForwardRequest(string address, int port)
        {
            string request = address == null ? $"tcp:{port}" : $"tcp:{port}:{address}";
            return FormAdbRequest(request);
        }

        private AdbSocket GetSocket()
        {
            return null;
        }

        public int GetAdbVersion()
        {
            throw new NotImplementedException();
        }

        public void KillAdb()
        {
            throw new NotImplementedException();
        }

        List<DeviceData> IAdbClient.GetDevices()
        {
            throw new NotImplementedException();
        }

        public int CreateForward(DeviceData device, string local, string remote, bool allowRebind) =>
            CreateForward(device, local?.ToString(), remote?.ToString(), allowRebind);

        public int CreateReverseForward(DeviceData device, string remote, string local, bool allowRebind)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            string rebind = allowRebind ? string.Empty : "norebind:";

            socket.SendAdbRequest($"reverse:forward:{rebind}{remote};{local}");
            _ = socket.ReadAdbResponse();
            _ = socket.ReadAdbResponse();
            string portString = socket.ReadString();

            return portString != null && int.TryParse(portString, out int port) ? port : 0;
        }

        public void RemoveReverseForward(DeviceData device, string remote)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest($"reverse:killforward:{remote}");
            AdbResponse response = socket.ReadAdbResponse();
        }

        public void RemoveAllReverseForwards(DeviceData device)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest($"reverse:killforward-all");
            AdbResponse response = socket.ReadAdbResponse();
        }

        public void RemoveForward(DeviceData device, int localPort)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SendAdbRequest($"host-serial:{device.Serial}:killforward:tcp:{localPort}");
            AdbResponse response = socket.ReadAdbResponse();
        }

        public void RemoveAllForwards(DeviceData device)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SendAdbRequest($"host-serial:{device.Serial}:killforward-all");
            AdbResponse response = socket.ReadAdbResponse();
        }

        public void ExecuteRemoteCommand(string command, DeviceData device, IShellOutputReceiver receiver) =>
            ExecuteRemoteCommand(command, device, receiver, Encoding);
        public void ExecuteRemoteCommand(string command, DeviceData device, IShellOutputReceiver receiver, Encoding encoding)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);

            socket.SetDevice(device);
            socket.SendAdbRequest($"shell:{command}");
            AdbResponse response = socket.ReadAdbResponse();

            try
            {
                using StreamReader reader = new StreamReader(socket.GetShellStream(), encoding);
                while (true)
                {
                    string line = reader.ReadLine();

                    if (line == null) { break; }

                    receiver?.AddOutput(line);
                }
            }
            finally
            {
                receiver?.Flush();
            }
        }

        public void Reboot(string into, DeviceData device)
        {
            EnsureDevice(device);

            string request = $"reboot:{into}";

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(request);
            AdbResponse response = socket.ReadAdbResponse();
        }

        public bool Connect(DnsEndPoint endpoint)
        {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            if (socket.Connected == false) return false;
            socket.SendAdbRequest($"host:connect:{endpoint.Host}:{endpoint.Port}");
            AdbResponse response = socket.ReadAdbResponse();
            string results = socket.ReadString();
            if (results == null || results.IndexOf("connected") == -1) return false;
            return true;


        }

        public bool Disconnect(DnsEndPoint endpoint)
        {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SendAdbRequest($"host:disconnect:{endpoint.Host}:{endpoint.Port}");
            AdbResponse response = socket.ReadAdbResponse();
            string results = socket.ReadString();
            if (results == null || results.IndexOf("disconnect") == -1) return false;
            return true;
        }

        public void Root(DeviceData device) => Root("root:", device);

        protected void Root(string request, DeviceData device)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(request);
            AdbResponse response = socket.ReadAdbResponse();

            byte[] buffer = new byte[1024];
            int read = socket.Read(buffer);

            string responseMessage = Encoding.UTF8.GetString(buffer, 0, read);

            if (responseMessage.IndexOf("restarting", StringComparison.OrdinalIgnoreCase) == -1)
            {
                throw new AdbException(responseMessage);
            }
            else
            {
                Utilities.Delay(3000).GetAwaiter().GetResult();
            }
        }

        public void Unroot(DeviceData device) => Root("unroot:", device);

        public void Install(DeviceData device, Stream apk, params string[] arguments)
        {
            EnsureDevice(device);

            if (apk == null)
            {
                throw new ArgumentNullException(nameof(apk));
            }

            if (!apk.CanRead || !apk.CanSeek)
            {
                throw new ArgumentOutOfRangeException(nameof(apk), "The apk stream must be a readable and seekable stream");
            }

            StringBuilder requestBuilder = new StringBuilder();
            requestBuilder.Append("exec:cmd package 'install' ");

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    requestBuilder.Append(' ');
                    requestBuilder.Append(argument);
                }
            }

            requestBuilder.Append($" -S {apk.Length}");

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest(requestBuilder.ToString());
            AdbResponse response = socket.ReadAdbResponse();

            byte[] buffer = new byte[32 * 1024];
            int read = 0;

            while ((read = apk.Read(buffer, 0, buffer.Length)) > 0)
            {
                socket.Send(buffer, read);
            }

            read = socket.Read(buffer);
            string value = Encoding.UTF8.GetString(buffer, 0, read);

            if (!value.Contains("Success"))
            {
                throw new AdbException(value);
            }
        }

        public void InstallMultiple(DeviceData device, Stream[] splitAPKs, string packageName, params string[] arguments)
        {
            EnsureDevice(device);

            if (packageName == null)
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            string session = InstallCreate(device, packageName, arguments);

            int i = 0;
            foreach (Stream splitAPK in splitAPKs)
            {
                if (splitAPK == null || !splitAPK.CanRead || !splitAPK.CanSeek)
                {
                    Debug.WriteLine("The apk stream must be a readable and seekable stream");
                    continue;
                }

                try
                {
                    InstallWrite(device, splitAPK, $"{nameof(splitAPK)}{i++}", session);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            InstallCommit(device, session);
        }

        public void InstallMultiple(DeviceData device, Stream baseAPK, Stream[] splitAPKs, params string[] arguments)
        {
            EnsureDevice(device);

            if (baseAPK == null)
            {
                throw new ArgumentNullException(nameof(baseAPK));
            }

            if (!baseAPK.CanRead || !baseAPK.CanSeek)
            {
                throw new ArgumentOutOfRangeException(nameof(baseAPK), "The apk stream must be a readable and seekable stream");
            }

            string session = InstallCreate(device, null, arguments);

            InstallWrite(device, baseAPK, nameof(baseAPK), session);

            int i = 0;
            foreach (Stream splitAPK in splitAPKs)
            {
                if (splitAPK == null || !splitAPK.CanRead || !splitAPK.CanSeek)
                {
                    Debug.WriteLine("The apk stream must be a readable and seekable stream");
                    continue;
                }

                try
                {
                    InstallWrite(device, splitAPK, $"{nameof(splitAPK)}{i++}", session);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            InstallCommit(device, session);
        }

        public string InstallCreate(DeviceData device, string packageName = null, params string[] arguments)
        {
            EnsureDevice(device);

            StringBuilder requestBuilder = new StringBuilder();
            requestBuilder.Append("exec:cmd package 'install-create' ");
            requestBuilder.Append(packageName.IsNullOrWhiteSpace() ? string.Empty : $"-p {packageName}");

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    requestBuilder.Append(' ');
                    requestBuilder.Append(argument);
                }
            }

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest(requestBuilder.ToString());
            AdbResponse response = socket.ReadAdbResponse();

            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = reader.ReadLine();

            if (!result.Contains("Success"))
            {
                throw new AdbException(reader.ReadToEnd());
            }

            int arr = result.IndexOf("]") - 1 - result.IndexOf("[");
            string session = result.Substring(result.IndexOf("[") + 1, arr);
            return session;
        }

        public void InstallWrite(DeviceData device, Stream apk, string apkName, string session)
        {
            EnsureDevice(device);

            if (apk == null)
            {
                throw new ArgumentNullException(nameof(apk));
            }

            if (!apk.CanRead || !apk.CanSeek)
            {
                throw new ArgumentOutOfRangeException(nameof(apk), "The apk stream must be a readable and seekable stream");
            }

            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (apkName == null)
            {
                throw new ArgumentNullException(nameof(apkName));
            }

            StringBuilder requestBuilder = new StringBuilder();
            requestBuilder.Append($"exec:cmd package 'install-write' ");

            requestBuilder.Append($" -S {apk.Length}");

            requestBuilder.Append($" {session} {apkName}.apk");

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest(requestBuilder.ToString());
            AdbResponse response = socket.ReadAdbResponse();

            byte[] buffer = new byte[32 * 1024];
            int read = 0;

            while ((read = apk.Read(buffer, 0, buffer.Length)) > 0)
            {
                socket.Send(buffer, read);
            }

            read = socket.Read(buffer);
            string value = Encoding.UTF8.GetString(buffer, 0, read);

            if (!value.Contains("Success"))
            {
                throw new AdbException(value);
            }
        }

        public void InstallCommit(DeviceData device, string session)
        {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);

            socket.SendAdbRequest($"exec:cmd package 'install-commit' {session}");
            AdbResponse response = socket.ReadAdbResponse();

            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = reader.ReadLine();
            if (!result.Contains("Success"))
            {
                throw new AdbException(reader.ReadToEnd());
            }
        }

        public void SendText(DeviceData device, string text)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(string.Format("shell:input text {0}", text));
            //AdbResponse response = socket.ReadAdbResponse();
            //using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            //if (reader.ReadToEnd().ToUpper().Contains("ERROR"))
            //{
            //   // throw new InvalidTextException();
            //}
        }

        protected void EnsureDevice(DeviceData device)
        {
        }

        public void Pull(DeviceData device, string remotePath, Stream stream, EventHandler<SyncProgressChangedEventArgs> syncProgressEventHandler = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            using ISyncService service = Factories.SyncServiceFactory(this, device);
            if (syncProgressEventHandler != null)
            {
                service.SyncProgressChanged += syncProgressEventHandler;
            }

            service.Pull(remotePath, stream, progress, cancellationToken);
        }

        public void Push(DeviceData device, string remotePath, Stream stream, int permissions, DateTimeOffset timestamp, EventHandler<SyncProgressChangedEventArgs> syncProgressEventHandler = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            using ISyncService service = Factories.SyncServiceFactory(this, device);
            if (syncProgressEventHandler != null)
            {
                service.SyncProgressChanged += syncProgressEventHandler;
            }

            service.Push(stream, remotePath, permissions, timestamp, progress, cancellationToken);
        }

        public bool CreateFolder(DeviceData device, string folderPath)
        {

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(string.Format("shell:mkdir {0}", folderPath));
            AdbResponse response = socket.ReadAdbResponse();
            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = reader.ReadToEnd();
            if (result.ToUpper().Contains("ERROR"))
            {
                return false;
            }

            return true;
        }
        public bool Screencap(DeviceData device, string savePath)
        {

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(string.Format("shell:screencap -p {0}", savePath));
            AdbResponse response = socket.ReadAdbResponse();
            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = reader.ReadToEnd();
            if (result.ToUpper().Contains("ERROR"))
            {
                return false;
            }

            return true;

        }

        public void SendKeyEvent(DeviceData device, int keycode)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            socket.SetDevice(device);
            socket.SendAdbRequest(string.Format("shell:input keyevent {0}", keycode));
            AdbResponse response = socket.ReadAdbResponse();
            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            if (reader.ReadToEnd().ToUpper().Contains("ERROR"))
            {
            }
        }
    }
}
