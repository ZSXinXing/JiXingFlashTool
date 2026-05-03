using JiXingFlashTool.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public class CommandModel
    {
        public string Name { get; set; }
        public int Tag { get; set; }

        //文件的路径，如果需要选择文件的路径的话
        public string FilePath { get; set; }
        /// <summary>
        /// 限制选择文件的格式
        /// </summary>
        public string FileFilter { get; set; }
        public bool NeedSelectFile { get; set; }

        public CommandModel(string name,int tag) {
            this.Name = name;
            this.Tag = tag;
        }

        public CommandModel(string name, TWRPCommandType type)
        {
            this.Name = name;
            this.Tag = (int)type;
        }
    }
}
