using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public class AdbMultCommandModel
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 执行的指令
        /// </summary>
        public List<string> CommandList { get; set; }
        /// <summary>
        /// 是否是自定义指令
        /// </summary>
        public bool IsCustom { get; set; }
        public AdbMultCommandModel(string name, List<string> commandList)
        {
            this.Name = name;
            this.CommandList = commandList;
        }
    }
}
