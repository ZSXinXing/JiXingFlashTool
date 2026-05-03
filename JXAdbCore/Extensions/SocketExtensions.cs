using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Extensions
{
    internal static  class SocketExtensions
    {
        public static Task<int> ReceiveAsync(this Socket socket, byte[] buffer,int offset,int size,SocketFlags socketFlags,CancellationToken cancellationToken)
        {
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() => socket.Close());

            TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>(socket);

            socket.BeginReceive(buffer, offset, size, socketFlags, delegate (IAsyncResult iar)
            {
                TaskCompletionSource<int> taskCompletionSource2 = (TaskCompletionSource<int>)iar.AsyncState;
                Socket socket2 = (Socket)taskCompletionSource2.Task.AsyncState;

                try
                {
                    taskCompletionSource2.TrySetResult(socket2.EndReceive(iar));
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException && cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource2.TrySetCanceled();
                    }
                    else
                    {
                        taskCompletionSource2.TrySetException(ex);
                    }
                }
                finally
                {
                    cancellationTokenRegistration.Dispose();
                }
            }, taskCompletionSource);

            return taskCompletionSource.Task;
        }
    }
}
