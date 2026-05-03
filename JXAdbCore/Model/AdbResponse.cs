using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Model
{
    public class AdbResponse
    {
        public AdbResponse() => Message = string.Empty;
        public static AdbResponse OK { get; } = new AdbResponse()
        {
            IOSuccess = true,
            Okay = true,
            Message = string.Empty,
            Timeout = false
        };
        public bool IOSuccess { get; set; }
        public bool Okay { get; set; }
        public bool Timeout { get; set; }
        public string Message { get; set; }

        public static AdbResponse FromError(string message) => new AdbResponse()
        {
            IOSuccess = true,
            Message = message,
            Okay = false,
            Timeout = false
        };

        public override bool Equals(object obj) =>
            obj is AdbResponse other
                && other.IOSuccess == IOSuccess
                && string.Equals(other.Message, Message, StringComparison.OrdinalIgnoreCase)
                && other.Okay == Okay
                && other.Timeout == Timeout;

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + IOSuccess.GetHashCode();
            hash = (hash * 23) + Message == null ? 0 : Message.GetHashCode();
            hash = (hash * 23) + Okay.GetHashCode();
            hash = (hash * 23) + Timeout.GetHashCode();

            return hash;
        }

        public override string ToString() => Equals(OK) ? "OK" : $"Error: {Message}";
    }
}
