using HandyControl.Controls;
using HandyControl.Tools;
using JiXingFlashTool.Model;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace JiXingFlashTool.Utils
{
    public class CommonTool
    {

        /// <summary>
        /// 获取本地网卡IP
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public static string GetLocalIPv4(NetworkInterfaceType _type)
        {
            string output = "";
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output = ip.Address.ToString();
                        }
                    }
                }
            }
            return output;
        }

        public static string GetPCIPV4()
        {
            string ipv4 = GetLocalIPv4(NetworkInterfaceType.Ethernet);
            if (ipv4 == "")
            {
                ipv4 = GetLocalIPv4(NetworkInterfaceType.Wireless80211);
            }
            return ipv4;
        }

        public static EthernetSNModel GetPCIPV4SNModel()
        {
            string pcIP = CommonTool.GetPCIPV4();
            if (pcIP != null && pcIP.IsIp())
            {
                string[] ipArray = pcIP.Split('.');
                EthernetSNModel ethernetSN = new EthernetSNModel();
                ethernetSN.StartIP = string.Format("{0}.{1}.{2}.{3}", ipArray[0], ipArray[1], ipArray[2], 1);
                ethernetSN.EndIP = string.Format("{0}.{1}.{2}.{3}", ipArray[0], ipArray[1], ipArray[2], 254);
                ethernetSN.Port = 5555.ToString();
                return ethernetSN;
            }
            return null;
        }


        public static int GetAvailableTcpPort()
        {
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            socket.Listen(1);
            var ipEndPoint = (IPEndPoint)socket.LocalEndPoint;
            socket.Close();
            socket.Dispose();
            var port = ipEndPoint.Port;
            return port;
        }


        public static string GetSizeString(long size)
        {

            int GB = 1024 * 1024 * 1024;//定义GB的计算常量
            int MB = 1024 * 1024;//定义MB的计算常量
            int KB = 1024;//定义KB的计算常量
            string sizeString = "";
            if (size / GB >= 1)//如果当前Byte的值大于等于1GB
            {
                sizeString = string.Format("{0}GB", Math.Round(size / (float)GB, 2));
            }
            else if (size / MB >= 1)//如果当前Byte的值大于等于1MB
            {
                sizeString = String.Format("{0}MB", Math.Round(size / (float)MB, 2));
            }
            else if (size / KB >= 1)//如果当前Byte的值大于等于1KB
            {
                sizeString = string.Format("{0}KB", Math.Round(size / (float)KB, 2));
            }
            else
            {
                sizeString = size.ToString();
            }
            return sizeString;
        }

        public static string ByteToBase64String(byte[] data)
        {
            string base64String = Convert.ToBase64String(data, 0, data.Length);
            base64String = base64String.Replace("data:image/png;base64,", "").Replace("data:image/jgp;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");
            return base64String;
        }

        public static int RandomIndex(int startIndex, int endIndex)
        {
            return new Random().Next(startIndex, endIndex);
        }

        public static bool MatchProbability(int probability)
        {
            return RandomIndex(1, 100) <= probability;
        }

        public static void OpenFolder(string folderPath)
        {

            if (string.IsNullOrEmpty(folderPath)) return;

            Process process = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe");
            psi.Arguments = folderPath;
            process.StartInfo = psi;
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                process?.Close();


            }
        }
    }
}
