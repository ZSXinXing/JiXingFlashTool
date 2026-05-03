using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Models
{
    public class FileModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Size { get; set; }
        public string Extension { get; set; }
        public bool IsApk {
            get {
                return Extension.ToLower().IndexOf("apk") != -1;
            }
        }

        public bool NeedInstall { get; set; } = true;
        public bool UsePhoneApk { get; set; } = true;
        public string FileNameWithoutExtension { get; set; }

    }
}
