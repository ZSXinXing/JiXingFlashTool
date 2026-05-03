using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.EventArg
{
    public class CastScreenManageResultEventArgs : CastScreenResultEventArgs
    {
        public CastScreenService CastScreenService;
        public CastScreenManageResultEventArgs(DeviceModel device, SocketPackageModel socketPackage, CastScreenService service) : base(device, socketPackage)
        {
            CastScreenService = service;
        }

        public static CastScreenManageResultEventArgs NewFormSuper(CastScreenResultEventArgs super, CastScreenService castScreenService)
        {
            return new CastScreenManageResultEventArgs(super.Device, super.SocketPackage, castScreenService);
        }
    }
}
