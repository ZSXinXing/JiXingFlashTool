using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public partial interface IAdbSocket
    {
        Task SendAsync(byte[] data, int length, CancellationToken cancellationToken);
        Task SendAsync(byte[] data, int offset, int length, CancellationToken cancellationToken);
        Task SendAdbRequestAsync(string request, CancellationToken cancellationToken);
        Task ReadAsync(byte[] data, CancellationToken cancellationToken);
        Task<int> ReadAsync(byte[] data, int length, CancellationToken cancellationToken);
        Task<string> ReadStringAsync(CancellationToken cancellationToken);
        Task<AdbResponse> ReadAdbResponseAsync(CancellationToken cancellationToken);
        Task SetDeviceAsync(DeviceData device, CancellationToken cancellationToken);
    }
}
