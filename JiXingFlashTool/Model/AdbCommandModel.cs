using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Models
{
    public class AdbCommandModel
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 执行的指令
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// 是否是自定义指令
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// 是否需要root权限 
        /// </summary>
        public bool NeedRoot { get; set; } = false;
        public AdbCommandModel(string name,string command) {
            this.Name = name;
            this.Command = command;
        }
    }
}
