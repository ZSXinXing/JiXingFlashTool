using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Models
{
    public class DeviceInstallModel
    {
        public DeviceModel Device { get; set; }
        public List<FileModel> FileList { get; set; }

        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int SumCount { get; set; }
        public int Progress { get; set; }
    }
}
