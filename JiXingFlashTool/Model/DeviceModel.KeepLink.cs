using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public partial class DeviceModel
    {
        /// <summary>
        /// 即使已经断开连接但是还是保持连接状态就是不断开链接
        /// </summary>
        public bool IsKeepLink { get; set; }
    }
}
