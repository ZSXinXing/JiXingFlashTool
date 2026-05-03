using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class CommandLineClient
    {
        private string path;
        public CommandLineClient(string path) {
            this.path = path;
        }

        public void StartServer()
        {
            CMD("start-server");
        }

        private string CMD(string cmdString)
        {
            Process p = new Process();
            p.StartInfo.FileName = path;
            p.StartInfo.Arguments = cmdString;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            string result = p.StandardOutput.ReadToEnd();
            p.Close();
            return result;
        }
    }

}


