using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.Model
{
    public class AppConfigModel
    {
        public int ScreenMiniWidth { get; set; }
        public int ScreenMaxWidth { get; set; }
        public int ScreenCurrentWidth { get; set; }
        public List<int> MiniCastScreenResolutionDefaultList = new List<int>() { 96, 144, 240, 320, 480, 720, 1080 };
        public List<int> MiniCastScreenRateDefaultList = new List<int>() { 4, 5, 10, 15, 20, 25, 30, 45, 60 };
        public List<int> ControlCastScreenResolutionDefaultList = new List<int>() { 320, 480, 720, 1080 };
        public List<int> ControlCastScreenRateDefaultList = new List<int>() { 25, 30, 45, 60 };

        public int MiniCastScreenResolution = 160;
        public int MiniCastScreenRate = 4;
        public int ControlCastScreenResolution = 720;
        public int ControlCastScreenRate = 45;
    }
}
