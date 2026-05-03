using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Model
{
    public class FileStatistics
    {
        public string Path { get; set; }
        public UnixFileMode FileMode { get; set; }
        public int Size { get; set; }

        public DateTimeOffset Time { get; set; }
        public override string ToString() => Path;
    }
}
