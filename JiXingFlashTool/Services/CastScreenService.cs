using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Utils;
using System;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JiXingFlashTool.EventArg;
using System.Windows.Threading;

namespace JiXingFlashTool.Services
{
    public class CastScreenService : ObservableObject
    {
        public event EventHandler<ScreenD3DEventArgs> D3DChanage;
        public event EventHandler<CastScreenResultEventArgs> ScreenResult;

        private Thread receiveSteamThread { get; set; }
        private CancellationTokenSource cts { get; set; }
        private byte[] packetBuf;
        private ArrayPool<byte> _pool;
        private ArrayPool<byte> pool
        {
            get
            {
                if (_pool == null) _pool = ArrayPool<byte>.Shared;
                return _pool;
            }
        }
        private YuvModel yuvModel { get; set; }
        private DeviceModel device { get; set; }
        private D3DImageSource d3dIS;
        public D3DImageSource D3dIS { get { return d3dIS; } }

        private SocketPackageModel socketPackage { get; set; }
        private VideoStreamDecoder videoStreamDecoder;

        public int VideoWidth
        {
            get
            {
                if (socketPackage == null) return 0;
                return socketPackage.VideoWidth;
            }
        }
        public int VideoHeight
        {
            get
            {
                if (socketPackage == null) return 0;
                return socketPackage.VideoHeight;
            }
        }

        public CastScreenService(DeviceModel device, SocketPackageModel socketPackage)
        {
            this.device = device;
            this.socketPackage = socketPackage;
        }

        public CastScreenService(DeviceModel device)
        {
            this.device = device;
        }


        public void Start(int resolution, int bate, int rate)
        {
            new Thread(() =>
            {
                try
                {
                    //获取电脑可用端口
                    int port = CommonTool.GetAvailableTcpPort();
                    AdbService.Instance.RemoveAllReverseForwards(device);
                    AdbService.Instance.CreateReverse(device, StaticConstant.PortApp, port.ToString());
                    AdbService.Instance.Push(device, StaticConstant.ScrcpyServerPCPath, StaticConstant.ScrcpyServerPhonePath);


                    new Thread(() =>
                    {
                        AdbService.Instance.ExecuteShellCommand(device, ScrcpyRunParameter(resolution, bate, rate), null);
                    }).Start();
                    TCSocketService socketService = new TCSocketService(port);
                    socketService.Start();
                    SocketPackageModel socketPackage = socketService.AccecptSocketClient();
                    if (socketPackage == null) socketService.Stop();
                    else
                    {
                        this.socketPackage = socketPackage;
                        StartReceiveStream();
                    }

                    if (ScreenResult != null) ScreenResult(this, new CastScreenResultEventArgs(device, socketPackage));
                }
                catch (Exception ex)
                {

                }

            }).Start();
        }

        public void ResetParamter(int resolution, int bate, int rate)
        {
            new Thread(() =>
            {
                try
                {
                    //获取电脑可用端口
                    int port = CommonTool.GetAvailableTcpPort();
                    AdbService.Instance.RemoveAllReverseForwards(device);
                    AdbService.Instance.CreateReverse(device, StaticConstant.PortApp, port.ToString());

                    new Thread(() =>
                    {
                        AdbService.Instance.ExecuteShellCommand(device, ScrcpyRunParameter(resolution, bate, rate), null);
                    }).Start();
                    TCSocketService socketService = new TCSocketService(port);
                    socketService.Start();
                    SocketPackageModel socketPackage = socketService.AccecptSocketClient();
                    if (socketPackage == null) socketService.Stop();
                    else this.socketPackage = socketPackage;
                    if (socketPackage != null) StartReceiveStream();
                }
                catch (Exception ex)
                {

                }

            }).Start();
        }

        public void StartReceiveStream()
        {
            try
            {

                if (receiveSteamThread != null)
                {
                    receiveSteamThread.Abort();
                    receiveSteamThread = null;
                }

                if (cts != null)
                {
                    cts.Cancel();
                    cts = null;
                }

                cts = new CancellationTokenSource();
                receiveSteamThread = new Thread(ReceiveStream);
                receiveSteamThread.IsBackground = true;
                receiveSteamThread.Start();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("StartReceiveStream,出现异常");
            }


        }

        public void StopReceiveStream()
        {

            if (receiveSteamThread != null) receiveSteamThread.Abort();

            if (cts != null) cts.Cancel();

            if (socketPackage.VideoClient != null) socketPackage.VideoClient.Close();

            if (socketPackage.ControlClient != null) socketPackage.ControlClient.Close();

        }

        private void ReceiveStream()
        {
            TcpClient tcpClient = socketPackage.VideoClient;

            while (!cts.Token.IsCancellationRequested)
            {

                try
                {
                    if (tcpClient.Connected == false) break;

                    NetworkStream videoStream = tcpClient.GetStream();

                    if (videoStream.DataAvailable && videoStream.CanRead)
                    {
                        var metaBuf = pool.Rent(12);

                        int bytesRead = videoStream.Read(metaBuf, 0, 12);

                        if (bytesRead == 0)
                        {
                            Debug.WriteLine("recovery stream 1 bytesRead = 0  ");
                            return;
                        }


                        if (bytesRead != 12)
                        {

                            Debug.WriteLine("recovery stream  bytesRead = 12  ");

                            return;
                        }


                        var metaSpan = metaBuf.AsSpan();
                        var packetSize = BinaryPrimitives.ReadInt32BigEndian(metaSpan[8..]);
                        packetBuf = pool.Rent(packetSize);
                        var pos = 0;
                        var bytesToRead = packetSize;

                        while (bytesToRead != 0 && !cts.Token.IsCancellationRequested)
                        {

                            bytesRead = videoStream.Read(packetBuf, pos, bytesToRead);

                            if (bytesRead == 0)
                            {
                                Debug.WriteLine("recovery stream 1 bytesRead = 2  ");
                                return;
                            }

                            pos += bytesRead;
                            bytesToRead -= bytesRead;
                        }

                        if (packetBuf.Length > 0)
                        {
                            DecodeVideo(packetBuf, (ret) => {
                            });
                        }
                        else
                        {
                        }
                        packetBuf = null;
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }

                Thread.Sleep(1);


            }
        }

        private void DecodeVideo(byte[] bytes, Action<bool> retAction)
        {

            if (videoStreamDecoder == null) videoStreamDecoder = new VideoStreamDecoder();

            videoStreamDecoder.Decode(bytes, (YuvModel yuvModel) =>
            {
                //如果屏幕发生旋转(width发生改变)
                if (yuvModel != null)
                {
                    if (this.yuvModel != null)
                    {

                        if (this.yuvModel.width != yuvModel.width && this.yuvModel.height != yuvModel.height)
                        {

                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                SynchronizationContext.SetSynchronizationContext(new
                                    DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                                SynchronizationContext.Current.Post(pl =>
                                {
                                    if (d3dIS != null)
                                    {
                                        d3dIS.Dispose();
                                        d3dIS = null;
                                    }
                                    d3dIS = new D3DImageSource(yuvModel.width, yuvModel.height, FrameFormat.YV12);

                                    if (D3DChanage != null)
                                        D3DChanage(this, new ScreenD3DEventArgs(device, yuvModel.width, yuvModel.height));
                                }, null);
                            });
                        }

                    }

                    Render(yuvModel);

                    this.yuvModel = yuvModel;

                    retAction(true);
                }
                else
                {
                    retAction(false);
                }


            });
        }


        private void Render(YuvModel yuvModel)
        {

            ThreadPool.QueueUserWorkItem(delegate
            {
                SynchronizationContext.SetSynchronizationContext(new
                    DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher));
                SynchronizationContext.Current.Post(pl =>
                {
                    try
                    {
                        if (d3dIS == null && (VideoHeight != 0 && VideoWidth != 0))
                        {
                            d3dIS = new D3DImageSource(VideoWidth, VideoHeight, FrameFormat.YV12);

                            if (D3DChanage != null)
                                D3DChanage(this, new ScreenD3DEventArgs(device, VideoWidth, VideoHeight));
                        }

                        d3dIS.Present(yuvModel.data, yuvModel.lineSize);
                    }
                    catch (Exception ex)
                    {

                    }
                }, null);
            });

        }

        public void SendMessage(byte[] buffer, int offset, int size)
        {

            try
            {
                if (socketPackage.ControlClient.Connected == false) return;
                socketPackage.ControlClient.GetStream().Write(buffer, offset, size);
            }
            catch (Exception ex)
            {

            }
        }

        #region 启动screen部分
        private string ScrcpyRunParameter(int resolution, int bate, int rate)
        {
            var cmds = new List<string>
                {
                    $"CLASSPATH={StaticConstant.ScrcpyServerPhonePath}",
                    "app_process",
                    "/",
                    "com.genymobile.scrcpy.Server",
                     "1.23",
                    "log_level=debug",
                    $"bit_rate={bate * 1024 * 1024}"
                };
            cmds.Add($"max_size={resolution}");
            cmds.Add($"max_fps={rate}");
            cmds.Add("tunnel_forward=false");
            cmds.Add("display_id=0");
            cmds.Add($"show_touches=False");
            cmds.Add($"stay_awake=True");
            cmds.Add("power_off_on_close=false");
            cmds.Add("downsize_on_error=true");
            cmds.Add("cleanup=true");

            string command = string.Join(" ", cmds);
            return command;
        }
        #endregion
    }
}
