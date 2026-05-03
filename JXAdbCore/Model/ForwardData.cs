using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Model
{
    public class ForwardData
    {
        public string SerialNumber { get; set; }
        public string Local { get; set; }
        public ForwardSpec LocalSpec => ForwardSpec.Parse(Local);

        public string Remote { get; set; }
        public ForwardSpec RemoteSpec => ForwardSpec.Parse(Remote);
        public static ForwardData FromString(string value)
        {
            if (value == null)
            {
                return null;
            }

            string[] parts = value.Split(' ');
            return new ForwardData()
            {
                SerialNumber = parts[0],
                Local = parts[1],
                Remote = parts[2]
            };
        }

        /// <inheritdoc/>
        public override string ToString() => $"{SerialNumber} {Local} {Remote}";
    }
}
