using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class AdbException : Exception
    {
        internal AdbException(String message) : base(message)
        {
        }
    }

    public class AdbInvalidResponseException : AdbException
    {
        internal AdbInvalidResponseException(String response) : base($"The server returned an invalid response '{response}'")
        {
        }
    }
}
