using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public interface IAdbCommandLineClient
    {
        Version GetVersion();
        void StartServer();
        bool IsValidAdbFile(string adbPath);
    }
}
