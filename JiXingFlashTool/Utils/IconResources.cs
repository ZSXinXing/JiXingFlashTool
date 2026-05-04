using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace JiXingFlashTool.Utils
{
    /// <summary>
    /// 图标资源入口，统一加载维护区和 TWRP 区按钮图标。
    /// </summary>
    public static class IconResources
    {
        /// <summary>
        /// 维护区“重启”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceRestartIcon = Load("Resource/Image/Maintenance/restart.png");

        /// <summary>
        /// 维护区“关机”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceShutdownIcon = Load("Resource/Image/Maintenance/shutdown.png");

        /// <summary>
        /// 维护区“打开手电筒”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceFlashOnIcon = Load("Resource/Image/Maintenance/flash_on.png");

        /// <summary>
        /// 维护区“关闭手电筒”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceFlashOffIcon = Load("Resource/Image/Maintenance/flash_off.png");

        /// <summary>
        /// 维护区“执行 Shell”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceShellIcon = Load("Resource/Image/Maintenance/shell.png");

        /// <summary>
        /// 维护区“刷入 ROOT”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceRootIcon = Load("Resource/Image/Maintenance/root.png");

        /// <summary>
        /// 维护区“检查系统更新”图标。
        /// </summary>
        public static readonly ImageSource MaintenanceUpdateIcon = Load("Resource/Image/Maintenance/update.png");

        /// <summary>
        /// TWRP 专业指令“重启到系统”图标。
        /// </summary>
        public static readonly ImageSource TwrpProRebootSystemIcon = Load("Resource/Image/TwrpPro/professional_reboot.png");

        /// <summary>
        /// TWRP 专业指令“双清”图标。
        /// </summary>
        public static readonly ImageSource TwrpProWipeIcon = Load("Resource/Image/TwrpPro/professional_wipe.png");

        /// <summary>
        /// TWRP 专业指令“清除系统”图标。
        /// </summary>
        public static readonly ImageSource TwrpProClearSystemIcon = Load("Resource/Image/TwrpPro/professional_clear_system.png");

        /// <summary>
        /// TWRP 专业指令“格式化 Data”图标。
        /// </summary>
        public static readonly ImageSource TwrpProFormatDataIcon = Load("Resource/Image/TwrpPro/professional_format_data.png");

        /// <summary>
        /// TWRP 专业指令“进入侧载”图标。
        /// </summary>
        public static readonly ImageSource TwrpProSideloadIcon = Load("Resource/Image/TwrpPro/professional_sideload.png");

        /// <summary>
        /// TWRP 专业指令“刷入文件”图标。
        /// </summary>
        public static readonly ImageSource TwrpProFlashFileIcon = Load("Resource/Image/TwrpPro/professional_flash_file.png");

        /// <summary>
        /// TWRP 专业指令“更新内核”图标。
        /// </summary>
        public static readonly ImageSource TwrpProUpdateKernelIcon = Load("Resource/Image/TwrpPro/professional_update_kernel.png");

        /// <summary>
        /// TWRP 专业指令“更新 TWRP”图标。
        /// </summary>
        public static readonly ImageSource TwrpProUpdateTwrpIcon = Load("Resource/Image/TwrpPro/professional_update_twrp.png");

        /// <summary>
        /// TWRP 专业指令“解密”图标。
        /// </summary>
        public static readonly ImageSource TwrpProDecryptIcon = Load("Resource/Image/TwrpPro/professional_decrypt.png");

        /// <summary>
        /// TWRP 专业指令“关闭开发者”图标。
        /// </summary>
        public static readonly ImageSource TwrpProDisableDeveloperIcon = Load("Resource/Image/TwrpPro/professional_disable_developer.png");

        /// <summary>
        /// TWRP 专业指令“跳过向导”图标。
        /// </summary>
        public static readonly ImageSource TwrpProSkipGuideIcon = Load("Resource/Image/TwrpPro/professional_skip_guide.png");

        /// <summary>
        /// 加载图标资源，兼容 PNG 和 SVG 文本文件。
        /// </summary>
        /// <param name="relativePath">相对资源路径。</param>
        /// <returns>图像资源。</returns>
        private static ImageSource Load(string relativePath)
        {
            var resourceStream = OpenResourceStream(relativePath);
            if (resourceStream == null)
            {
                return null;
            }

            using (resourceStream)
            {
                var bytes = ReadAllBytes(resourceStream);
                if (IsPng(bytes))
                {
                    return LoadPng(bytes);
                }

                var text = System.Text.Encoding.UTF8.GetString(bytes);
                if (text.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadSvg(text);
                }
            }

            return null;
        }

        /// <summary>
        /// 打开 WPF 资源流。
        /// </summary>
        private static Stream OpenResourceStream(string relativePath)
        {
            try
            {
                var resourceUri = new Uri($"pack://application:,,,/JiXingFlashTool;component/{relativePath}", UriKind.Absolute);
                var resourceInfo = Application.GetResourceStream(resourceUri);
                return resourceInfo?.Stream;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 读取流中的全部字节。
        /// </summary>
        private static byte[] ReadAllBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 判断是否为 PNG 文件。
        /// </summary>
        private static bool IsPng(byte[] bytes)
        {
            return bytes != null
                   && bytes.Length >= 8
                   && bytes[0] == 0x89
                   && bytes[1] == 0x50
                   && bytes[2] == 0x4E
                   && bytes[3] == 0x47
                   && bytes[4] == 0x0D
                   && bytes[5] == 0x0A
                   && bytes[6] == 0x1A
                   && bytes[7] == 0x0A;
        }

        /// <summary>
        /// 加载 PNG 图标。
        /// </summary>
        private static ImageSource LoadPng(byte[] bytes)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(bytes);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// 加载 SVG 文本并转换为 DrawingImage。
        /// </summary>
        private static ImageSource LoadSvg(string svgText)
        {
            var svgDocument = XDocument.Parse(svgText);
            var svgNamespace = svgDocument.Root?.Name.Namespace;
            var drawingGroup = new DrawingGroup();

            foreach (var pathNode in svgDocument.Descendants(svgNamespace + "path"))
            {
                var pathData = pathNode.Attribute("d")?.Value;
                if (string.IsNullOrWhiteSpace(pathData))
                {
                    continue;
                }

                var geometry = Geometry.Parse(pathData);
                var strokeBrush = ParseBrush(pathNode.Attribute("stroke")?.Value);
                var fillBrush = ParseBrush(pathNode.Attribute("fill")?.Value);
                var strokeWidth = ParseDouble(pathNode.Attribute("stroke-width")?.Value, 1.0);

                if (strokeBrush != null)
                {
                    var pen = new Pen(strokeBrush, strokeWidth)
                    {
                        StartLineCap = PenLineCap.Round,
                        EndLineCap = PenLineCap.Round,
                        LineJoin = PenLineJoin.Round
                    };
                    drawingGroup.Children.Add(new GeometryDrawing(null, pen, geometry));
                }
                else
                {
                    drawingGroup.Children.Add(new GeometryDrawing(fillBrush ?? Brushes.White, null, geometry));
                }
            }

            foreach (var ellipseNode in svgDocument.Descendants(svgNamespace + "ellipse"))
            {
                var centerX = ParseDouble(ellipseNode.Attribute("cx")?.Value, 0);
                var centerY = ParseDouble(ellipseNode.Attribute("cy")?.Value, 0);
                var radiusX = ParseDouble(ellipseNode.Attribute("rx")?.Value, 0);
                var radiusY = ParseDouble(ellipseNode.Attribute("ry")?.Value, 0);
                var fillBrush = ParseBrush(ellipseNode.Attribute("fill")?.Value) ?? Brushes.White;
                drawingGroup.Children.Add(new GeometryDrawing(fillBrush, null, new EllipseGeometry(new Point(centerX, centerY), radiusX, radiusY)));
            }

            var image = new DrawingImage(drawingGroup);
            image.Freeze();
            return image;
        }

        /// <summary>
        /// 解析 SVG 颜色值。
        /// </summary>
        private static Brush ParseBrush(string brushText)
        {
            if (string.IsNullOrWhiteSpace(brushText))
            {
                return null;
            }

            var value = brushText.Trim();
            if (value.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (value.StartsWith("var(", StringComparison.OrdinalIgnoreCase))
            {
                var fallbackStart = value.IndexOf(',');
                var fallbackEnd = value.LastIndexOf(')');
                if (fallbackStart > 0 && fallbackEnd > fallbackStart)
                {
                    value = value.Substring(fallbackStart + 1, fallbackEnd - fallbackStart - 1).Trim();
                }
                else
                {
                    return Brushes.White;
                }
            }

            try
            {
                return (Brush)new BrushConverter().ConvertFromString(value);
            }
            catch
            {
                return Brushes.White;
            }
        }

        /// <summary>
        /// 解析 SVG 数值。
        /// </summary>
        private static double ParseDouble(string valueText, double defaultValue)
        {
            return double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }
    }
}
