using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JXAdbCore.Receivers
{
    public class ConsoleOutputReceiver : MultiLineReceiver
    {
        private const RegexOptions DefaultRegexOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;

        private readonly StringBuilder output = new StringBuilder();
        public ConsoleOutputReceiver()
        {
        }

        public override string ToString() => output.ToString();

        public void ThrowOnError(string line)
        {
            if (!ParsesErrors)
            {
                if (line.EndsWith(": not found"))
                {
                    throw new FileNotFoundException($"The remote execution returned: '{line}'");
                }

                if (line.EndsWith("No such file or directory"))
                {
                    throw new FileNotFoundException($"The remote execution returned: '{line}'");
                }

                if (line.Contains("Unknown option"))
                {
                    //throw new UnknownOptionException($"The remote execution returned: '{line}'");
                }

                // for "aborting" commands
                if (Regex.IsMatch(line, "Aborting.$", DefaultRegexOptions))
                {
                    ///throw new CommandAbortingException($"The remote execution returned: '{line}'");
                }

                if (Regex.IsMatch(line, "applet not found$", DefaultRegexOptions))
                {
                    throw new FileNotFoundException($"The remote execution returned: '{line}'");
                }


                if (Regex.IsMatch(line, "(permission|access) denied$", DefaultRegexOptions))
                {
                   // throw new PermissionDeniedException($"The remote execution returned: '{line}'");
                }
            }
        }

        protected override void ProcessNewLines(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("$"))
                {
                    continue;
                }

                output.AppendLine(line);
            }
        }
    }
}
