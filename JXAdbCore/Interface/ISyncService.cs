using JXAdbCore.EventHandler;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Interface
{
    public interface ISyncService : IDisposable
    {
        bool IsOpen { get; }
        void Push(Stream stream, string remotePath, int permissions, DateTimeOffset timestamp, IProgress<int> progress, CancellationToken cancellationToken);

        void Pull(string remotePath, Stream stream, IProgress<int> progress, CancellationToken cancellationToken);
        FileStatistics Stat(string remotePath);
        IEnumerable<FileStatistics> GetDirectoryListing(string remotePath);
        void Open();
        event EventHandler<SyncProgressChangedEventArgs> SyncProgressChanged;
    }
}
