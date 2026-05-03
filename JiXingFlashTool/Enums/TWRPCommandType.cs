using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Enums
{
    /// <summary>
    /// TWRP操作类型
    /// </summary>
    public enum TWRPCommandType
    {
        /// <summary>
        /// 重启到系统
        /// </summary>
        RebootSystem = 0,
        /// <summary>
        /// 重启到TWRP
        /// </summary>
        RebootTWRP,
        /// <summary>
        /// 双清
        /// </summary>
        Wipe,
        /// <summary>
        /// 清除系统
        /// </summary>
        ClearSystem,
        /// <summary>
        /// 格式化
        /// </summary>
        Format,
        /// <summary>
        /// 开启侧载
        /// </summary>
        Sideload,
        /// <summary>
        /// 刷入文件
        /// </summary>
        FlashFile,
        /// <summary>
        /// 刷入内核
        /// </summary>
        FlashKernel,
        /// <summary>
        /// 更新TWRP
        /// </summary>
        UpdateTWRP,
        /// <summary>
        /// 解密
        /// </summary>
        Decryption,
        /// <summary>
        /// 检查解密状态
        /// </summary>
        CheckDecryption,
        /// <summary>
        /// 检查指令是否执行成功
        /// </summary>
        CheckCommandResult,
    }
}
