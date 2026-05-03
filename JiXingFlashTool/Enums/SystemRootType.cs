using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Enums
{
    public enum SystemRootType
    {
        /// <summary>
        /// 没有Root权限
        /// </summary>
        None = -1,
        /// <summary>
        /// adb root
        /// </summary>
        Shell,
        /// <summary>
        /// Kernelsu
        /// </summary>
        Kernelsu
    }
}
