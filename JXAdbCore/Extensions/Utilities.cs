using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore.Extensions
{
    internal static class Utilities
    {
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken = default) => Task.Run(function, cancellationToken);

        public static Task Delay(int dueTime, CancellationToken cancellationToken = default) =>Task.Delay(dueTime, cancellationToken);

        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrWhiteSpace(value);

        public static string Join(string separator, IEnumerable<string> values) =>string .Join(separator, values);

        public static DateTimeOffset FromUnixTimeSeconds(long seconds) =>DateTimeOffset.FromUnixTimeSeconds(seconds);
    }
}
