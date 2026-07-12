using HandyControl.Controls;
using JiXingFlashTool.Delegate;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Model;
using JiXingFlashTool.Utils;
using JXAdbCore;
using JXAdbCore.Enums;
using JXAdbCore.EventHandler;
using JXAdbCore.Interface;
using JXAdbCore.Model;
using JXAdbCore.Receivers;
using Microsoft.VisualBasic;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace JiXingFlashTool.Services
{
    public class AdbService
    {
        private DeviceMonitor monitor;
        private AdbServer server;
        private AdbClient client;

        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<DeviceEventArgs> DeviceChanged;
        public event EventHandler<AdbEventArgs> FinishADBService;

        public AdbCommandLineDelegate adbCommandLineDelegate;

        public DeviceEventArgs TempDeviceEventArgs;

        private AdbService()
        {
        }

        public static AdbService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly AdbService instance = new AdbService();
        }

        /// <summary>
        /// 断开网络 ADB 设备，用于清理 adb devices 中残留的 offline 网络连接。
        /// </summary>
        /// <param name="serial">网络 ADB 序列号，格式为 ip:port。</param>
        /// <returns>成功发起断开返回 true。</returns>
        public bool DisconnectDevice(string serial)
        {
            if (!TryCreateNetworkEndpoint(serial, out DnsEndPoint endpoint))
            {
                return false;
            }

            try
            {
                return Disconnect(endpoint);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据网络 ADB 序列号创建网络终结点。
        /// </summary>
        /// <param name="serial">网络 ADB 序列号。</param>
        /// <param name="endpoint">解析出的网络终结点。</param>
        /// <returns>解析成功返回 true。</returns>
        private static bool TryCreateNetworkEndpoint(string serial, out DnsEndPoint endpoint)
        {
            endpoint = null;
            if (string.IsNullOrWhiteSpace(serial))
            {
                return false;
            }

            string[] parts = serial.Split(':');
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out _) ||
                !int.TryParse(parts[1], out int port))
            {
                return false;
            }

            endpoint = new DnsEndPoint(parts[0], port);
            return true;
        }

        public void StartServiceAsync()
        {

            ThreadPool.QueueUserWorkItem((x) =>
            {
                try
                {

                    //开启服务
                    AdbServer server = AdbServer.Instance;
                    server.StartServer(StaticConstant.PathAdb);
                    DeviceService.Instance.StartDeviceMonitor(new DeviceMonitor((new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)))));
                    client = new AdbClient();
                    if (this.FinishADBService != null)
                    {
                        this.FinishADBService(this, new AdbEventArgs());
                    }
                }
                catch(Exception ex) {
                    Growl.Error(ex.Message);
                }

            });
        }

        public List<DeviceModel> Devices()
        {
            List<DeviceModel> devices = new List<DeviceModel>();

            if (client == null) return devices;

            List<DeviceData> list = client.GetDevices();

            foreach (DeviceData deviceData in list)
            {
                devices.Add(new DeviceModel(deviceData));
            }
            return devices;
        }

        public bool Disconnect(DnsEndPoint endpoint)
        {
            return client.Disconnect(endpoint);
        }

        public bool Connect(DnsEndPoint endpoint)
        {
            return client.Connect(endpoint);
        }

        public System.Drawing.Size GetScreenSize(DeviceData device)
        {
            string sizeStr = ExecuteRemoteCommand("wm size", device);
            string sizeInfo = sizeStr.ToString().Replace("Physical size: ", "").Replace("\r\n", "");
            if (sizeInfo.IndexOf("x") != -1)
            {
                string[] sizes = sizeInfo.Split('x');
                return new System.Drawing.Size(int.Parse(sizes[0]), int.Parse(sizes[1]));
            }
            else
            {
                return System.Drawing.Size.Empty;
            }
        }

        public string GetAndroidVersion(DeviceModel device) => GetProp(device,"ro.build.version.release");

        /// <summary>
        /// 获取系统编译日期，优先使用标准 Unix 时间戳属性并格式化为年月日。
        /// </summary>
        /// <param name="device">目标设备。</param>
        /// <returns>格式为 yyyy-MM-dd 的编译日期；无法获取时返回空字符串。</returns>
        public string GetBuildDate(DeviceModel device)
        {
            string buildTimestamp = GetProp(device, "ro.build.date.utc")?.Trim();
            if (long.TryParse(buildTimestamp, out long unixTimestamp))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime.ToString("yyyy-MM-dd");
                }
                catch (ArgumentOutOfRangeException)
                {
                    return string.Empty;
                }
            }

            string buildDate = GetProp(device, "ro.build.date")?.Trim();
            return DateTime.TryParse(buildDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime date)
                ? date.ToString("yyyy-MM-dd")
                : string.Empty;
        }


        public string GetRoSerialno(DeviceModel device) => GetProp(device, "ro.serialno");
        public string GetBuildUser(DeviceModel device) => GetProp(device, "ro.build.user");
        public string GetProp(DeviceModel device,string propKey) {
            return ExecuteRemoteCommand(string.Format("getprop {0}",propKey), device);
        }

        public string GetSettingGlobal(DeviceModel device, string key)
        {
            return ExecuteRemoteCommand(string.Format("settings get global {0}", key), device);
        }

        public void Push(DeviceData device, string filePath, string remotePath)
        {

            using (Stream stream = File.OpenRead(filePath))
            {
                client.Push(device, remotePath, stream, 777, DateTimeOffset.Now, null);
            }

        }

        /// <summary>
        /// 推送本地文件到设备，并回传进度。
        /// </summary>
        /// <param name="device">目标设备。</param>
        /// <param name="filePath">本地文件路径。</param>
        /// <param name="remotePath">设备端目标路径。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void Push(DeviceData device, string filePath, string remotePath, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                client.Push(device, remotePath, stream, 777, DateTimeOffset.Now, null, progress, cancellationToken);
            }
        }

        public void Push(DeviceData device, Stream fileStream, string remotePath)
        {
            client.Push(device, remotePath, fileStream, 777, DateTimeOffset.Now, null);
        }

        /// <summary>
        /// 推送文件流到设备，并回传进度。
        /// </summary>
        /// <param name="device">目标设备。</param>
        /// <param name="fileStream">文件流。</param>
        /// <param name="remotePath">设备端目标路径。</param>
        /// <param name="progress">进度回调。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        public void Push(DeviceData device, Stream fileStream, string remotePath, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            client.Push(device, remotePath, fileStream, 777, DateTimeOffset.Now, null, progress, cancellationToken);
        }

        public void SendText(DeviceModel device, string text)
        {
            client.SendText(device, text);
        }

        public async void Pull(DeviceModel device, string devicePath, string pcPath)
        {
            using (Stream stream = File.OpenWrite(pcPath))
            {
                AutoResetEvent autoResetEvent = new AutoResetEvent(false);

                client.Pull(device, devicePath, stream, null, new Progress<int>((percent) =>
                {
                    if (percent >= 100) autoResetEvent.Set();
                }));
                await Task.Run(() =>
                {
                    autoResetEvent.WaitOne();
                });
            }
        }

        public string ExecuteRemoteCommand(string command, DeviceModel device)
        {
            string resultStr = string.Empty;
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            try
            {
                client.ExecuteRemoteCommand(command, device, receiver, Encoding.UTF8);
                resultStr = receiver.ToString();
            }
            catch (Exception ex)
            {
                resultStr = ex.ToString();
            }
            return resultStr;
        }


        public string Read(DeviceData device,string path) {
            string context = ExecuteRemoteCommand($"cat {path}",device,false);
            return context;
        }

        public void ExecuteShellCommand(DeviceData device, string command, IShellOutputReceiver receiver)
        {
            client.ExecuteRemoteCommand(command, device, receiver);
        }

        public Task ExecuteRemoteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, CancellationToken cancellationToken = default)
        {
            return client.ExecuteRemoteCommandAsync(command, device, receiver, cancellationToken);
        }

        public Task ExecuteCommandAsync(string command, DeviceData device, IShellOutputReceiver receiver, CancellationToken cancellationToken = default)
        {
            return client.ExecuteCommandAsync(command, device, receiver, cancellationToken);
        }

        public void RemoveAllReverseForwards(DeviceData device)
        {
            client.RemoveAllReverseForwards(device);
        }

        public void CreateReverseForward(DeviceData device, int localPort, int remotePort, bool allowReBind = true)
        {
            client.CreateReverseForward(device, $"tcp:{localPort}", $"tcp:{remotePort}", allowReBind);
        }

        public int CreateReverse(DeviceData device, string portApp, string portServer)
        {
            return client.CreateReverseForward(device, $"localabstract:{portApp}", $"tcp:{portServer}", true);
        }

        public bool CreateFile(DeviceData device, string context, string filePath, bool isForce = false)
        {
            if (isForce) client.ExecuteRemoteCommand(string.Format("rm -f {0}", filePath), device, null);

            client.ExecuteRemoteCommand($"touch {filePath}", device, null);
            string shellString = string.Format("cat>{0} <<END\n{1} \nEND", filePath, context);
            client.ExecuteRemoteCommand(shellString, device, null);
            return true;
        }

        public bool CreateFolder(DeviceData device, string folder)
        {
            if(!FolderExist(device,folder)) client.ExecuteRemoteCommand($"mkdir {folder}", device, null);
            return true;
        }

        public bool FolderExist(DeviceData device, string folder) => Exist(device, folder);

        public bool FileExist(DeviceData device,string filePath) => Exist(device, filePath);
        public long FileSize(DeviceData device, string path)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            client.ExecuteRemoteCommand($"ls -l {path}", device, receiver);
            string sizeString = receiver.ToString();
            if (sizeString == null || sizeString.Equals(string.Empty) || sizeString.Length == 0) return 0;
            try
            {
                string[] dataInfo = sizeString.Split(' ');
                sizeString = dataInfo[4];
                long size = Convert.ToInt64(sizeString);
                return size;
            }
            catch
            {
                return 0;
            }
        }
        public bool Exist(DeviceData device, string path) {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            client.ExecuteRemoteCommand($"ls -l {path}", device, receiver);
            return receiver.ToString().IndexOf("No such file or directory") == -1;
        }

        public void RefreshFile(DeviceData device, string filePath) {
            string updateMediaCmd = string.Format("am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d file:///{0}", filePath);
            ExecuteShellCommand(device, updateMediaCmd, null);
        }

        private string ExecuteRemoteCommand(string command, DeviceData device,bool ReplaceNewline = true)
        {
            ConsoleOutputReceiver receiver = new ConsoleOutputReceiver();
            client.ExecuteRemoteCommand(command, device,receiver);
            if (ReplaceNewline) return receiver.ToString().Replace("\r\n", "");
            else return receiver.ToString();
        }

        public void SendKeyEvent(DeviceData device, JXAdbCore.Enums.AndroidKeycode keycode) {
            client.SendKeyEvent(device, (int)keycode);
        }

        public void InstallFromPhone(DeviceData device,string apkPath) {
            client.ExecuteRemoteCommand(string.Format("pm install {0}", apkPath), device, null);
        }

        public void Install(DeviceData device, Stream apk, params string[] arguments) => client.Install(device, apk, arguments);
        public Task InstallAsync(DeviceData device, Stream apk, CancellationToken cancellationToken, params string[] arguments) => client.InstallAsync(device, apk, cancellationToken, arguments);
        public void Back(DeviceModel device) => client.SendKeyEvent(device,(int)JXAdbCore.Enums.AndroidKeycode.AKEYCODE_BACK);
        public void Home(DeviceModel device) => client.SendKeyEvent(device, (int)JXAdbCore.Enums.AndroidKeycode.AKEYCODE_HOME);
        public void Menu(DeviceModel device) => client.SendKeyEvent(device, (int)JXAdbCore.Enums.AndroidKeycode.AKEYCODE_APP_SWITCH);


        public string SideloadFile(DeviceModel device, string filePath, AdbCommandLineDelegate adbDelegate = null) {
            return CMDExcute($"-s {device.Serial} sideload {filePath}",adbDelegate);
        }

        public string SumMD5(DeviceModel device, string filePath)
        {
            string result = ExecuteRemoteCommand($"md5sum {filePath}", device);
            string pattern = @"([a-f0-9]{32})(?=\s.*)";
            Regex regex = new Regex(pattern);
            System.Text.RegularExpressions.Match match = regex.Match(result);
            string md5 = string.Empty;
            if (match.Success) md5 = match.Groups[0].Value;
            return md5;
        }

        #region 原生CMD
        public string CMDExcute(string arguments, AdbCommandLineDelegate adbDelegate = null)
        {
            try
            {
                string cmd = StaticConstant.PathAdb;
                Process p = new Process();
                p.StartInfo.FileName = cmd;   
                p.StartInfo.Arguments = arguments;  
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.StartInfo.UseShellExecute = false;   
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true; 
                p.StartInfo.RedirectStandardError = true; 
                p.StartInfo.CreateNoWindow = true;  
                p.Start();
                string line = null;
                while (!p.StandardOutput.EndOfStream)
                {
                    string oneLine = p.StandardOutput.ReadLine() + Environment.NewLine;
                    if (adbDelegate != null) adbDelegate(oneLine);
                    line += oneLine;
                }
                Console.WriteLine(line);
                p.WaitForExit();
                p.Close();
                return line;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        #endregion
    }
}
