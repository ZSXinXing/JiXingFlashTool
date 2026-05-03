using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class AdbServer : IAdbServer
    {
        private readonly IAdbClient adbClient;
        public static AdbServer Instance { get; set; } = new AdbServer();

        public AdbServer()
        {
            this.adbClient = new AdbClient();
        }

        #region 功能方法

        public bool StartServer(string adbPath,bool restart = false) {

            if (adbPath == null || adbPath.Length == 0 || adbPath.ToLower().IndexOf("adb.exe") == -1)
                throw new ArgumentNullException("ADB路径不存在");

            CommandLineClient commandLineClient = new CommandLineClient(adbPath);
            AdbServerStatus serverStatus;
            try
            {
                serverStatus = GetStatus();
            }
            catch (Exception ex)
            {
                serverStatus = new AdbServerStatus();
                serverStatus.IsRunning = false;
            }

            if (serverStatus.IsRunning == false)
            {
                commandLineClient.StartServer();
            }
            else if (serverStatus.IsRunning == true && restart == true) {
                commandLineClient.StartServer();
            }

            try
            {
                serverStatus = GetStatus();
            }
            catch {
                return false;
            }

            return true;
        }

        public void RestartServer()
        {
        }


        public AdbServerStatus GetStatus()
        {
            try
            {
                int versionCode = adbClient.GetAdbVersion();

                return new AdbServerStatus()
                {
                    IsRunning = true,
                    Version = new Version(1, 0, versionCode)
                };
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    return new AdbServerStatus()
                    {
                        IsRunning = false,
                        Version = null
                    };
                }
                else
                {
                    throw ex;
                }
            }
        }


        #endregion
    }
}
