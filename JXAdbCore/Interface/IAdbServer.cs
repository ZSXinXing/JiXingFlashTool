using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public interface IAdbServer
    {
        bool StartServer(string adbPath, bool restart = false);

        void RestartServer();

        AdbServerStatus GetStatus();
    }
}
