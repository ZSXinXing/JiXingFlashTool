using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Model
{
    public class ForwardSpec
    {
        private static readonly Dictionary<string, ForwardProtocol> Mappings = new Dictionary<string, ForwardProtocol>(StringComparer.OrdinalIgnoreCase)
        {
            { "tcp", ForwardProtocol.Tcp },
            { "localabstract", ForwardProtocol.LocalAbstract },
            { "localreserved", ForwardProtocol.LocalReserved },
            { "localfilesystem", ForwardProtocol.LocalFilesystem },
            { "dev", ForwardProtocol.Device },
            { "jdwp", ForwardProtocol.JavaDebugWireProtocol }
        };
        public ForwardProtocol Protocol { get; set; }

        public int Port { get; set; }

        public string SocketName { get; set; }

        public int ProcessId { get; set; }

        public static ForwardSpec Parse(string spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            string[] parts = spec.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(spec));
            }

            if (!Mappings.ContainsKey(parts[0]))
            {
                throw new ArgumentOutOfRangeException(nameof(spec));
            }

            ForwardProtocol protocol = Mappings[parts[0]];

            ForwardSpec value = new ForwardSpec()
            {
                Protocol = protocol
            };


            bool isInt = int.TryParse(parts[1], out int intValue);

            switch (protocol)
            {
                case ForwardProtocol.JavaDebugWireProtocol:
                    if (!isInt)
                    {
                        throw new ArgumentOutOfRangeException(nameof(spec));
                    }

                    value.ProcessId = intValue;
                    break;

                case ForwardProtocol.Tcp:
                    if (!isInt)
                    {
                        throw new ArgumentOutOfRangeException(nameof(spec));
                    }

                    value.Port = intValue;
                    break;

                case ForwardProtocol.LocalAbstract:value.SocketName = parts[1];break;
                case ForwardProtocol.LocalFilesystem: value.SocketName = parts[1]; break;
                case ForwardProtocol.LocalReserved: value.SocketName = parts[1]; break;
                case ForwardProtocol.Device: value.SocketName = parts[1]; break;

            }

            return value;
        }

        public override string ToString()
        {
            string protocolString = Mappings.FirstOrDefault(v => v.Value == Protocol).Key;

            //return Protocol switch
            //{
            //    ForwardProtocol.JavaDebugWireProtocol => $"{protocolString}:{ProcessId}",
            //    ForwardProtocol.Tcp => $"{protocolString}:{Port}",
            //    ForwardProtocol.LocalAbstract
            //    or ForwardProtocol.LocalFilesystem
            //    or ForwardProtocol.LocalReserved
            //    or ForwardProtocol.Device => $"{protocolString}:{SocketName}",
            //    _ => string.Empty,
            //};
            return protocolString;
        }

        /// <inheritdoc/>
        public override int GetHashCode() =>
            (int)Protocol
                ^ Port
                ^ ProcessId
                ^ (SocketName == null ? 1 : SocketName.GetHashCode());

        /// <inheritdoc/>
        //public override bool Equals(object obj)
        //{
        //    if (obj.GetType() != typeof(ForwardSpec))
        //    {
        //        return false;
        //    }

        //    ForwardSpec other = (ForwardSpec)obj;
        //    if (other.Protocol != Protocol)
        //    {
        //        return false;
        //    }

        //    return Protocol switch
        //    {
        //        ForwardProtocol.JavaDebugWireProtocol => ProcessId == other.ProcessId,
        //        ForwardProtocol.Tcp => Port == other.Port,
        //        ForwardProtocol.LocalAbstract
        //        or ForwardProtocol.LocalFilesystem
        //        or ForwardProtocol.LocalReserved
        //        or ForwardProtocol.Device => string.Equals(SocketName, other.SocketName),
        //        _ => false,
        //    };
        //}
    }
}
