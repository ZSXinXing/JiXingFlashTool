using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public class YuvModel
    {
        public IntPtr[] data = new IntPtr[3];
        public int[] lineSize = new int[3];
        public int width;
        public int height;
        public YuvModel(IntPtr[] data, int[] lineSize, int width, int height)
        {
            this.data = data;
            this.lineSize = lineSize;
            this.width = width;
            this.height = height;
        }

        public void Free()
        {
            Array.Clear(data, 0, data.Length);
            Array.Clear(lineSize, 0, lineSize.Length);
        }
    }
}
