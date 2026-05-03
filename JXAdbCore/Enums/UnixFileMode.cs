using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Enums
{
    public enum UnixFileMode
    {
        TypeMask = 0x8000,
        Socket = 0xc000,
        SymbolicLink = 0xa000,
        Regular = 0x8000,
        Block = 0x6000,
        Directory = 0x4000,
        Character = 0x2000,
        FIFO = 0x1000
    }
}
