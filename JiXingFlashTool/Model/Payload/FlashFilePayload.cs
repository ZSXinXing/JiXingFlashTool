using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model.Payload
{
    /// <summary>
    /// 刷入文件Payload
    /// </summary>
    /// <param name="Device">手机</param>
    /// <param name="filePath">文件地址</param>
    public sealed record FlashFilePayload(DeviceModel Device, string filePath);
}
