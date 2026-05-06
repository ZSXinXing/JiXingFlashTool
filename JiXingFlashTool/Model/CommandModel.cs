using JiXingFlashTool.Enums;
using System.Windows;
using System.Windows.Media;

namespace JiXingFlashTool.Model
{
    /// <summary>
    /// TWRP 专业指令按钮模型，承载按钮文案、类型、图标和展示参数。
    /// </summary>
    public class CommandModel
    {
        /// <summary>
        /// 指令名称。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 指令类型标识。
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// 指令需要的文件路径。
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 指令的文件筛选器。
        /// </summary>
        public string FileFilter { get; set; }

        /// <summary>
        /// 指令是否需要选择文件。
        /// </summary>
        public bool NeedSelectFile { get; set; }

        /// <summary>
        /// 按钮背景画刷。
        /// </summary>
        public Brush BackgroundBrush { get; set; }

        /// <summary>
        /// 按钮前景画刷。
        /// </summary>
        public Brush ForegroundBrush { get; set; }

        /// <summary>
        /// 按钮宽度。
        /// </summary>
        public double ButtonWidth { get; set; }

        /// <summary>
        /// 按钮外边距。
        /// </summary>
        public Thickness ButtonMargin { get; set; }

        /// <summary>
        /// 按钮图标。
        /// </summary>
        public ImageSource IconSource { get; set; }

        /// <summary>
        /// 按钮文字边距。
        /// </summary>
        public Thickness TextMargin { get; set; }

        /// <summary>
        /// 使用整数类型初始化指令。
        /// </summary>
        /// <param name="name">指令名称。</param>
        /// <param name="tag">类型标识。</param>
        public CommandModel(string name, int tag)
        {
            Name = name;
            Tag = tag;
        }

        /// <summary>
        /// 使用 TWRP 类型初始化指令。
        /// </summary>
        /// <param name="name">指令名称。</param>
        /// <param name="type">TWRP 指令类型。</param>
        public CommandModel(string name, TWRPCommandType type)
        {
            Name = name;
            Tag = (int)type;
        }

        /// <summary>
        /// 使用 TWRP 类型和文件属性初始化指令。
        /// </summary>
        /// <param name="name">指令名称。</param>
        /// <param name="type">TWRP 指令类型。</param>
        /// <param name="needSelectFile">是否需要选择文件。</param>
        /// <param name="fileFilter">文件筛选器。</param>
        public CommandModel(string name, TWRPCommandType type, bool needSelectFile, string fileFilter = null)
        {
            Name = name;
            Tag = (int)type;
            NeedSelectFile = needSelectFile;
            FileFilter = fileFilter;
        }
    }
}
