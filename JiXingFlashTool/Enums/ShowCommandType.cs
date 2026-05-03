using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Enums
{
    /// <summary>
    /// 指令类型
    /// </summary>
    public enum ShowCommandType
    {
        /// <summary>
        /// 系统指令
        /// </summary>
        System = 0,
        /// <summary>
        /// TWRP指令
        /// </summary>
        TWRP = 1,
        /// <summary>
        /// 更新指令
        /// </summary>
        Update = 2,
    }
}
