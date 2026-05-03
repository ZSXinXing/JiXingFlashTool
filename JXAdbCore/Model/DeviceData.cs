using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows;
using System.Diagnostics;

namespace JXAdbCore.Model
{
    public class DeviceData
    {
        internal const string DeviceDataRegexString = @"^(?<serial>[a-zA-Z0-9_-]+(?:\s?[\.a-zA-Z0-9_-]+)?(?:\:\d{1,})?)\s+(?<state>device|connecting|offline|unknown|bootloader|recovery|download|sideload|authorizing|unauthorized|host|no permissions)(?<message>.*?)(\s+usb:(?<usb>[^:]+))?(?:\s+product:(?<product>[^:]+))?(\s+model\:(?<model>[\S]+))?(\s+device\:(?<device>[\S]+))?(\s+features:(?<features>[^:]+))?(\s+transport_id:(?<transport_id>[^:]+))?$";
        private static readonly Regex Regex = new Regex(DeviceDataRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public string Serial { get; set; }
        public DeviceState State { get; set; }
        public string Model { get; set; }
        public string Product { get; set; }
        public string Name { get; set; }
        public string Features { get; set; }
        public string Usb { get; set; }
        public string TransportId { get; set; }
        public string Message { get; set; }
        public static DeviceData CreateFromAdbData(string data)
        {
            Match m = Regex.Match(data);
            DeviceData device = m.Success
                ? new DeviceData()
                {
                    Serial = m.Groups["serial"].Value,
                    State = GetStateFromString(m.Groups["state"].Value),
                    Model = m.Groups["model"].Value,
                    Product = m.Groups["product"].Value,
                    Name = m.Groups["device"].Value,
                    Features = m.Groups["features"].Value,
                    Usb = m.Groups["usb"].Value,
                    TransportId = m.Groups["transport_id"].Value,
                    Message = m.Groups["message"].Value
                }
                : null;
            return device;
        }
        public override string ToString() => Serial;
        internal static DeviceState GetStateFromString(string state)
        {
            DeviceState value;

            if (string.Equals(state, "device", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Online;
            }
            else if (string.Equals(state, "no permissions", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.NoPermissions;
            }
            else if (string.Equals(state, "Offline", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Offline;
            }
            else if (string.Equals(state, "Recovery", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Recovery;
            }
            else if (string.Equals(state, "Sideload", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Sideload;
            }
            else if (string.Equals(state, "Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Unauthorized;
            }
            else if (string.Equals(state, "Authorizing", StringComparison.OrdinalIgnoreCase))
            {
                value = DeviceState.Authorizing;
            }
            else {
                value = DeviceState.Unknown;
            }

            return value;
        }
    }
}
