using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Receivers
{
    public interface IShellOutputReceiver
    {
        bool ParsesErrors { get; }
        void AddOutput(string line);
        void Flush();
    }
}
