using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Enums
{
    public enum CommandType : int
    {
        RebootSystem = 0,
        OpenFlashlight,
        CloseFlashlight,
        ExecuteShell,
        FlashMagisk,
        RebootRecovery,
        RebootDownload,
        WipeUserData,
        WipeSystem,
        Format,
        OpenSideload,
        FlashFile,
        Decryption,
        CloseDeveloperMode,
        Skip,
        SkipGuide
    }
}
