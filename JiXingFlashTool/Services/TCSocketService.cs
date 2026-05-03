using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace JiXingFlashTool.Services
{
    public class TCSocketService
    {
        private TcpListener _TcpListener;
        private TcpClient _VideoClient;
        public TcpClient VideoClient
        {
            get { return _VideoClient; }
        }
        private TcpClient _ControlClient;
        public TcpClient ControlClient
        {
            get { return _ControlClient; }
        }

        public TcpListener TcpListener
        {
            get
            {
                return _TcpListener;
            }
        }

        public readonly int Port;
        public TCSocketService(int port)
        {
            Port = port;
            _TcpListener = new TcpListener(IPAddress.Loopback, port);

            if (_TcpListener == null)
            {
                Debug.WriteLine("TcpListener 为null");
            }
        }

        public bool Start()
        {
            try
            {
                _TcpListener.Start();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TcpListener开始监听失败:" + ex.Message);
                return false;
            }
        }

        public void Stop()
        {
            if (_TcpListener != null)
            {
                _TcpListener.Stop();
                _TcpListener = null;

                if (VideoClient != null)
                {
                    VideoClient.Close();
                }

                if (ControlClient != null)
                {
                    ControlClient.Close();
                }
            }

        }


        private void VideoStreamClose(DeviceModel device)
        {

            //   DeviceHelper.Instance.RemoveDeviceRequest(device);

        }


        /// <summary>
        /// 接收从客户端发送过来的客户端
        /// </summary>
        /// <param name="timeout">等待超时，毫秒</param>
        /// <returns></returns>
        public SocketPackageModel AccecptSocketClient(int timeout = 4000)
        {

            if (_TcpListener == null)
            {
                Debug.WriteLine($"等待接受Socket客户失败，因为_TcpListener 为空");
                return null;
            }

            SocketPackageModel deviceSocketPackage = null;
            bool isWaitEnd = false;

            Thread waitThead = new Thread(() =>
            {
                try
                {

                    TcpClient videoClient = _TcpListener.AcceptTcpClient();
                    //  Debug.WriteLine($"接收到视频流Socket");
                    _VideoClient = videoClient;

                    TcpClient controlClient = _TcpListener.AcceptTcpClient();
                    //   Debug.WriteLine($"接收到控制流Socket");
                    _ControlClient = controlClient;


                    //读取视频流传输的头部
                    var infoStream = videoClient.GetStream();
                    infoStream.ReadTimeout = 2000;
                    byte[] infoBytes = new byte[68];

                    while (!infoStream.DataAvailable && isWaitEnd == false)
                    {
                        Thread.Sleep(1);
                    }

                    infoStream.Read(infoBytes, 0, infoBytes.Length);

                    if (infoBytes == null)
                    {
                        //   Debug.WriteLine($"视频参数头为空:");
                        isWaitEnd = true;
                    }


                    string content = Encoding.UTF8.GetString(infoBytes);
                    string model = Encoding.UTF8.GetString(infoBytes.Take(64).ToArray()).TrimEnd(new char[] { '\0' });
                    int videoWidth = BinaryPrimitives.ReadInt16BigEndian(infoBytes.Skip(64).Take(2).ToArray());
                    int videoHeight = BinaryPrimitives.ReadInt16BigEndian(infoBytes.Skip(66).Take(2).ToArray());

                    deviceSocketPackage = new SocketPackageModel(videoClient, controlClient, videoWidth, videoHeight, model);

                    isWaitEnd = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AccecptSocketClient等待客户端出错,Message:{ex.Message}");
                }
            });
            waitThead.IsBackground = true;
            waitThead.Start();

            DateTime initTime = DateTime.Now;
            while ((DateTime.Now - initTime).TotalMilliseconds <= timeout && isWaitEnd == false) Thread.Sleep(1);
            ////停止线程
            waitThead.Abort();

            return deviceSocketPackage;
        }

        public TcpClient AccecptSndCpySocketClient()
        {

            try
            {
                if (_TcpListener == null)
                {
                    Debug.WriteLine($"等待接受SndCpy音频流客户失败，因为_TcpListener 为空");
                    return null;
                }

                TcpClient videoClient = _TcpListener.AcceptTcpClient();
                Debug.WriteLine($"接收到SndCpy音频流Socket");
                return videoClient;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AccecptSocketClient等待客户端出错,Message:{ex.Message}");
            }
            return null;
        }

    }
}
