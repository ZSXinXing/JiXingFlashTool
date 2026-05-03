using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Extensions
{
    public static class CrossPlatformFunc
    {
        /// <summary>
        /// Determines whether the specified file exists.
        /// </summary>
        public static Func<string, bool> CheckFileExists = File.Exists;

        /// <summary>
        /// Runs process, invoking a specific command, and reads the standard output and standard error output.
        /// </summary>
        /// <returns>The return code of the process.</returns>
        public static Func<string, string, List<string>, List<string>, int> RunProcess = (string filename, string command, List<string> errorOutput, List<string> standardOutput) =>
        {
            ProcessStartInfo psi = new ProcessStartInfo(filename, command)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using Process process = Process.Start(psi);
            string standardErrorString = process.StandardError.ReadToEnd();
            string standardOutputString = process.StandardOutput.ReadToEnd();

            errorOutput?.AddRange(standardErrorString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            standardOutput?.AddRange(standardOutputString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            if (!process.WaitForExit(5000))
            {
                process.Kill();
            }

            return process.ExitCode;
        };
    }
}
