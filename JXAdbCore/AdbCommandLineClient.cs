using JXAdbCore.Extensions;
using JXAdbCore.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class AdbCommandLineClient : IAdbCommandLineClient
    {
        private const string AdbVersionPattern = "^.*(\\d+)\\.(\\d+)\\.(\\d+)$";
        public AdbCommandLineClient(string adbPath)
        {
            if (adbPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(adbPath));
            }

            EnsureIsValidAdbFile(adbPath);

            AdbPath = adbPath;
        }

        public string AdbPath { get; private set; }
        public Version GetVersion()
        {
            List<string> standardOutput = new List<string>();

            RunAdbProcess("version", null, standardOutput);

            Version version = GetVersionFromOutput(standardOutput) ?? throw new AdbException($"The version of the adb executable at {AdbPath} could not be determined.");

            return version;
        }

        public void StartServer()
        {
            int status = RunAdbProcessInner("start-server", null, null);

            if (status == 0)
            {
                return;
            }
            RunAdbProcess("start-server", null, null);
        }

        public bool IsValidAdbFile(string adbPath) => CrossPlatformFunc.CheckFileExists(adbPath);

        internal static Version GetVersionFromOutput(List<string> output)
        {
            foreach (string line in output)
            {
                // Skip empty lines
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                Match matcher = Regex.Match(line, AdbVersionPattern);
                if (matcher.Success)
                {
                    int majorVersion = int.Parse(matcher.Groups[1].Value);
                    int minorVersion = int.Parse(matcher.Groups[2].Value);
                    int microVersion = int.Parse(matcher.Groups[3].Value);

                    return new Version(majorVersion, minorVersion, microVersion);
                }
            }

            return null;
        }

        protected void RunAdbProcess(string command, List<string> errorOutput, List<string> standardOutput)
        {
            int status = RunAdbProcessInner(command, errorOutput, standardOutput);

            if (status != 0)
            {
                throw new AdbException($"The adb process returned error code {status} when running command {command}");
            }
        }
        protected int RunAdbProcessInner(string command, List<string> errorOutput, List<string> standardOutput)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            int status = CrossPlatformFunc.RunProcess(AdbPath, command, errorOutput, standardOutput);

            return status;
        }

        public static void EnsureIsValidAdbFile(string adbPath)
        {
            //if (client == null)
            //{
            //    throw new ArgumentNullException(nameof(client));
            //}

            //if (!client.IsValidAdbFile(adbPath))
            //{
            //    throw new FileNotFoundException($"The adb.exe executable could not be found at {adbPath}");
            //}
        }
    }
}
