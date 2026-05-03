using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Extensions
{
    public static class SyncCommandConverter
    {
        private static readonly Dictionary<SyncCommand, string> Values = new Dictionary<SyncCommand, string>();
        static SyncCommandConverter()
        {
            Values.Add(SyncCommand.DATA, "DATA");
            Values.Add(SyncCommand.DENT, "DENT");
            Values.Add(SyncCommand.DONE, "DONE");
            Values.Add(SyncCommand.FAIL, "FAIL");
            Values.Add(SyncCommand.LIST, "LIST");
            Values.Add(SyncCommand.OKAY, "OKAY");
            Values.Add(SyncCommand.RECV, "RECV");
            Values.Add(SyncCommand.SEND, "SEND");
            Values.Add(SyncCommand.STAT, "STAT");
        }

        public static byte[] GetBytes(SyncCommand command)
        {
            if (!Values.ContainsKey(command))
            {
                throw new ArgumentOutOfRangeException(nameof(command), $"{command} is not a valid sync command");
            }

            string commandText = Values[command];
            byte[] commandBytes = AdbClient.Encoding.GetBytes(commandText);

            return commandBytes;
        }

        public static SyncCommand GetCommand(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            string commandText = AdbClient.Encoding.GetString(value);

            SyncCommand? key = Values.Where(d => string.Equals(d.Value, commandText, StringComparison.OrdinalIgnoreCase)).Select(d => new SyncCommand?(d.Key)).SingleOrDefault();

            return key == null ? throw new ArgumentOutOfRangeException(nameof(value), $"{commandText} is not a valid sync command") : key.Value;
        }
    }
}
