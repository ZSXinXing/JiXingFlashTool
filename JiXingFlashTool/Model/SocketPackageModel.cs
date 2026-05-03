using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public class SocketPackageModel
    {
        private TcpClient videoClient;
        public TcpClient VideoClient { get { return videoClient; } }


        private TcpClient controlClient;
        public TcpClient ControlClient { get { return controlClient; } }

        private int videoWidth;

        public int VideoWidth { get { return videoWidth; } }


        private int videoHeight;
        public int VideoHeight { get { return videoHeight; } }


        private string model;
        public string Model { get { return model; } }

        public SocketPackageModel(TcpClient videoClient, TcpClient controlClient, int videoWidth, int videoHeight, string model)
        {
            this.videoClient = videoClient;
            this.controlClient = controlClient;
            this.videoWidth = videoWidth;
            this.videoHeight = videoHeight;
            this.model = model;
        }
    }
}
