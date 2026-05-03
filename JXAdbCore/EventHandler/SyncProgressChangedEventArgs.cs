using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.EventHandler
{
    public class SyncProgressChangedEventArgs : EventArgs
    {
        public double ProgressPercentage => TotalBytesToReceive != 0L ? ReceivedBytesSize * 100.0 / TotalBytesToReceive : 0.0;

        public long ReceivedBytesSize { get; internal set; }
        public long TotalBytesToReceive { get; internal set; }
        internal SyncProgressChangedEventArgs(long received, long total)
        {
            ReceivedBytesSize = received;
            TotalBytesToReceive = total;
        }
    }
}
