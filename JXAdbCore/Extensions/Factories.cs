using JXAdbCore.Interface;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Extensions
{
    public static class Factories
    {
        static Factories() => Reset();

        public static Func<EndPoint, IAdbSocket> AdbSocketFactory { get; set; }
        public static Func<EndPoint, IAdbClient> AdbClientFactory { get; set; }
        public static Func<string, IAdbCommandLineClient> AdbCommandLineClientFactory { get; set; }

        public static Func<IAdbClient, DeviceData, ISyncService> SyncServiceFactory { get; set; }

        public static void Reset()
        {
            AdbSocketFactory = (endPoint) => new AdbSocket(endPoint);
            AdbClientFactory = (endPoint) => new AdbClient(endPoint, AdbSocketFactory);
            AdbCommandLineClientFactory = (path) => new AdbCommandLineClient(path);
            SyncServiceFactory = (client, device) => new SyncService(client, device);
        }
    }
}
