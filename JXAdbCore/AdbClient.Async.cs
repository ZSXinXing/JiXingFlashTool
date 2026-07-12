using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using JXAdbCore.Model;
using JXAdbCore.Receivers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public partial class AdbClient
    {
        public async Task<int> GetAdbVersionAsync(CancellationToken cancellationToken) {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync("host:version", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            string version = await socket.ReadStringAsync(cancellationToken);

            return int.Parse(version, NumberStyles.HexNumber);
        }

        public async Task KillAdbAsync(CancellationToken cancellationToken) {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync("host:kill", cancellationToken);
        }

        public async Task<List<DeviceData>> GetDevicesAsync(CancellationToken cancellationToken) {
            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync("host:devices-l", cancellationToken);
            await socket.ReadAdbResponseAsync(cancellationToken);
            string reply = await socket.ReadStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(reply))
            {
                return new List<DeviceData>();
            }

            string[] data = reply.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            return data.Select(DeviceData.CreateFromAdbData).ToList();
        }

        public async Task<int> CreateForwardAsync(DeviceData device, string local, string remote, bool allowRebind, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            string rebind = allowRebind ? string.Empty : "norebind:";

            await socket.SendAdbRequestAsync($"host-serial:{device.Serial}:forward:{rebind}{local};{remote}", cancellationToken);
            await socket.ReadAdbResponseAsync(cancellationToken);
            await socket.ReadAdbResponseAsync(cancellationToken);
            string portString = await socket.ReadStringAsync(cancellationToken);

            return portString != null && int.TryParse(portString, out int port) ? port : 0;
        }

        public Task<int> CreateForwardAsync(DeviceData device, ForwardSpec local, ForwardSpec remote, bool allowRebind, CancellationToken cancellationToken) =>
            CreateForwardAsync(device, local?.ToString(), remote?.ToString(), allowRebind, cancellationToken);

        public async Task<int> CreateReverseForwardAsync(DeviceData device, string remote, string local, bool allowRebind, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            string rebind = allowRebind ? string.Empty : "norebind:";

            await socket.SendAdbRequestAsync($"reverse:forward:{rebind}{remote};{local}", cancellationToken);
            await socket.ReadAdbResponseAsync(cancellationToken);
            await socket.ReadAdbResponseAsync(cancellationToken);
            string portString = await socket.ReadStringAsync(cancellationToken);

            return portString != null && int.TryParse(portString, out int port) ? port : 0;
        }

        public async Task RemoveReverseForwardAsync(DeviceData device, string remote, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync($"reverse:killforward:{remote}", cancellationToken);
            AdbResponse response = socket.ReadAdbResponse();
        }

        public async Task RemoveAllReverseForwardsAsync(DeviceData device, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync($"reverse:killforward-all", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
        }

        public async Task RemoveForwardAsync(DeviceData device, int localPort, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host-serial:{device.Serial}:killforward:tcp:{localPort}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
        }

        public async Task RemoveAllForwardsAsync(DeviceData device, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host-serial:{device.Serial}:killforward-all", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
        }

        public async Task<IEnumerable<ForwardData>> ListForwardAsync(DeviceData device, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host-serial:{device.Serial}:list-forward", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            string data = await socket.ReadStringAsync(cancellationToken);

            string[] parts = data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Select(ForwardData.FromString);
        }

        public async Task<IEnumerable<ForwardData>> ListReverseForwardAsync(DeviceData device, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync($"reverse:list-forward", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            string data = await socket.ReadStringAsync(cancellationToken);

            string[] parts = data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            return parts.Select(ForwardData.FromString);
        }

        public Task ExecuteRemoteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, CancellationToken cancellationToken) =>
            ExecuteRemoteCommandAsync(command, device, receiver, Encoding, cancellationToken);

        public async Task ExecuteRemoteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, Encoding encoding, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            cancellationToken.Register(socket.Dispose);

            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync($"shell:{command}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            try
            {
                using StreamReader reader = new StreamReader(socket.GetShellStream(), encoding);
                while (!cancellationToken.IsCancellationRequested)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (line == null) { break; }

                    receiver?.AddOutput(line);
                }
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                   // throw new ShellCommandUnresponsiveException(e);
                }
            }
            finally
            {
                receiver?.Flush();
            }
        }

        public async Task ExecuteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, CancellationToken cancellationToken)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            cancellationToken.Register(socket.Dispose);

            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync($"host:{command}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            try
            {
                using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
                while (!cancellationToken.IsCancellationRequested)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (line == null) { break; }

                    receiver?.AddOutput(line);
                }
            }
            catch (Exception e)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    // throw new ShellCommandUnresponsiveException(e);
                }
            }
            finally
            {
                receiver?.Flush();
            }
        }

        public async Task RebootAsync(string into, DeviceData device, CancellationToken cancellationToken) {
            EnsureDevice(device);

            string request = $"reboot:{into}";

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync(request, cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
        }

        public async Task<string> PairAsync(DnsEndPoint endpoint, string code, CancellationToken cancellationToken) {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host:pair:{code}:{endpoint.Host}:{endpoint.Port}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            string results = await socket.ReadStringAsync(cancellationToken);
            return results;
        }

        public async Task<string> ConnectAsync(DnsEndPoint endpoint, CancellationToken cancellationToken) {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host:connect:{endpoint.Host}:{endpoint.Port}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            string results = await socket.ReadStringAsync(cancellationToken);
            return results;
        }

        public async Task<string> DisconnectAsync(DnsEndPoint endpoint, CancellationToken cancellationToken) {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SendAdbRequestAsync($"host:disconnect:{endpoint.Host}:{endpoint.Port}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            string results = await socket.ReadStringAsync(cancellationToken);
            return results;
        }

        public Task RootAsync(DeviceData device, CancellationToken cancellationToken) => RootAsync("root:", device, cancellationToken);

        public Task UnrootAsync(DeviceData device, CancellationToken cancellationToken) => RootAsync("unroot:", device, cancellationToken);
        protected async Task RootAsync(string request, DeviceData device, CancellationToken cancellationToken = default)
        {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync(request, cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

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

        public async Task InstallAsync(DeviceData device, Stream apk, CancellationToken cancellationToken, params string[] arguments)
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
                    _ = requestBuilder.Append(' ');
                    _ = requestBuilder.Append(argument);
                }
            }

            _ = requestBuilder.Append($" -S {apk.Length}");

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync(requestBuilder.ToString(), cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            byte[] buffer = new byte[32 * 1024];
            int read = 0;

            while ((read = await apk.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await socket.SendAsync(buffer, read, cancellationToken);
            }

            read = await socket.ReadAsync(buffer, buffer.Length, cancellationToken);
            string value = Encoding.UTF8.GetString(buffer, 0, read);

            if (!value.Contains("Success"))
            {
                throw new AdbException(value);
            }
        }

        public Task InstallAsync(DeviceData device, Stream apk, params string[] arguments) => InstallAsync(device, apk, default, arguments);
        public Task InstallMultipleAsync(DeviceData device, Stream[] splitAPKs, string packageName, params string[] arguments) =>
            InstallMultipleAsync(device, splitAPKs, packageName, default, arguments);

        public Task InstallMultipleAsync(DeviceData device, Stream baseAPK, Stream[] splitAPKs, params string[] arguments) =>
            InstallMultipleAsync(device, baseAPK, splitAPKs, default, arguments);

        public Task InstallMultipleAsync(DeviceData device, Stream[] splitAPKs, string packageName, CancellationToken cancellationToken, params string[] arguments) =>
            InstallMultipleAsync(device, splitAPKs, packageName, default, arguments);
        public async Task InstallMultipleAsync(DeviceData device, Stream baseAPK, Stream[] splitAPKs, CancellationToken cancellationToken, params string[] arguments) {
            EnsureDevice(device);

            if (baseAPK == null)
            {
                throw new ArgumentNullException(nameof(baseAPK));
            }

            if (!baseAPK.CanRead || !baseAPK.CanSeek)
            {
                throw new ArgumentOutOfRangeException(nameof(baseAPK), "The apk stream must be a readable and seekable stream");
            }

            string session = await InstallCreateAsync(device, null, cancellationToken, arguments);

            await InstallWriteAsync(device, baseAPK, nameof(baseAPK), session, cancellationToken);

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
                    await InstallWriteAsync(device, splitAPK, $"{nameof(splitAPK)}{i++}", session, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            await InstallCommitAsync(device, session, cancellationToken);
        }

        public Task<string> InstallCreateAsync(DeviceData device, string packageName, params string[] arguments) =>
            InstallCreateAsync(device, packageName, default, arguments);
        public async Task<string> InstallCreateAsync(DeviceData device, string packageName, CancellationToken cancellationToken, params string[] arguments) {
            EnsureDevice(device);

            StringBuilder requestBuilder = new StringBuilder();
            _ = requestBuilder.Append("exec:cmd package 'install-create' ");
            _ = requestBuilder.Append(packageName.IsNullOrWhiteSpace() ? string.Empty : $"-p {packageName}");

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    _ = requestBuilder.Append(' ');
                    _ = requestBuilder.Append(argument);
                }
            }

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync(requestBuilder.ToString(), cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = await reader.ReadLineAsync();

            if (!result.Contains("Success"))
            {
                throw new AdbException(await reader.ReadToEndAsync());
            }

            int arr = result.IndexOf("]") - 1 - result.IndexOf("[");
            string session = result.Substring(result.IndexOf("[") + 1, arr);
            return session;
        }

        public async Task InstallWriteAsync(DeviceData device, Stream apk, string apkName, string session, CancellationToken cancellationToken) {
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

            // add size parameter [required for streaming installs]
            // do last to override any user specified value
            requestBuilder.Append($" -S {apk.Length}");

            requestBuilder.Append($" {session} {apkName}.apk");

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync(requestBuilder.ToString(), cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            byte[] buffer = new byte[32 * 1024];
            int read = 0;
            while ((read = await apk.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await socket.SendAsync(buffer, read, cancellationToken);
            }

            read = await socket.ReadAsync(buffer, buffer.Length, cancellationToken);
            string value = Encoding.UTF8.GetString(buffer, 0, read);

            if (!value.Contains("Success"))
            {
                throw new AdbException(value);
            }
        }

        public async Task InstallCommitAsync(DeviceData device, string session, CancellationToken cancellationToken) {
                        using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);

            await socket.SendAdbRequestAsync($"exec:cmd package 'install-commit' {session}", cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);

            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = await reader.ReadLineAsync();
            if (!result.Contains("Success"))
            {
                throw new AdbException(await reader.ReadToEndAsync());
            }
        }

        public async Task SendKeyEventAsync(DeviceData device, string key, CancellationToken cancellationToken = default) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync(string.Format("shell:input keyevent {0}", key), cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = await reader.ReadToEndAsync();
            if (result.ToUpper().Contains("ERROR"))
            {
              //  throw new InvalidKeyEventException("KeyEvent is invalid");
            }
        }

        public async Task SendTextAsync(DeviceData device, string text, CancellationToken cancellationToken) {
            EnsureDevice(device);

            using IAdbSocket socket = adbSocketFactory(EndPoint);
            await socket.SetDeviceAsync(device, cancellationToken);
            await socket.SendAdbRequestAsync(string.Format("shell:input text {0}", text), cancellationToken);
            AdbResponse response = await socket.ReadAdbResponseAsync(cancellationToken);
            using StreamReader reader = new StreamReader(socket.GetShellStream(), Encoding);
            string result = await reader.ReadToEndAsync();
            if (result.ToUpper().Contains("ERROR"))
            {
               // throw new InvalidTextException();
            }
        }

        public async Task ClearInputAsync(DeviceData device, int charcount, CancellationToken cancellationToken) {
            await SendKeyEventAsync(device, "KEYCODE_MOVE_END");
            await ExecuteRemoteCommandAsync("input keyevent " + Utilities.Join(" ", Enumerable.Repeat("KEYCODE_DEL ", charcount)), device, null, cancellationToken);
        }
    }
}
