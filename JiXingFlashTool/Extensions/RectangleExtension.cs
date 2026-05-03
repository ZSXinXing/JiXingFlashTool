using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Extensions
{
    internal static class RectangleExtension
    {
        /// <summary>
        /// 在一个Rectangle获取一个随机坐标
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point RandomPoint(this Rectangle rectangle) {

            Random ran = new Random();
            int randomX = ran.Next(rectangle.X, rectangle.X + rectangle.Width);
            int randomY = ran.Next(rectangle.Y, rectangle.Y + rectangle.Height);

            return new Point(randomX, randomY);

        }

    }
}
