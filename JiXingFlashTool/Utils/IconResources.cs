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
    /// 鍥炬爣璧勬簮鍏ュ彛锛岀粺涓€鍔犺浇缁存姢鍖哄拰 TWRP 鍖烘寜閽浘鏍囥€?    /// </summary>
    public static class IconResources
    {
        /// <summary>
        /// 缁存姢鍖衡€滈噸鍚€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceRestartIcon = Load("Resources/Image/Maintenance/restart.png");

        /// <summary>
        /// 缁存姢鍖衡€滃叧鏈衡€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceShutdownIcon = Load("Resources/Image/Maintenance/shutdown.png");

        /// <summary>
        /// 缁存姢鍖衡€滄墦寮€鎵嬬數绛掆€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceFlashOnIcon = Load("Resources/Image/Maintenance/flash_on.png");

        /// <summary>
        /// 缁存姢鍖衡€滃叧闂墜鐢电瓛鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceFlashOffIcon = Load("Resources/Image/Maintenance/flash_off.png");

        /// <summary>
        /// 缁存姢鍖衡€滄墽琛?Shell鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceShellIcon = Load("Resources/Image/Maintenance/shell.png");

        /// <summary>
        /// 缁存姢鍖衡€滃埛鍏?ROOT鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceRootIcon = Load("Resources/Image/Maintenance/root.png");

        /// <summary>
        /// 缁存姢鍖衡€滄鏌ョ郴缁熸洿鏂扳€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource MaintenanceUpdateIcon = Load("Resources/Image/Maintenance/update.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滈噸鍚埌绯荤粺鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProRebootSystemIcon = Load("Resources/Image/TwrpPro/professional_reboot.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滃弻娓呪€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProWipeIcon = Load("Resources/Image/TwrpPro/professional_wipe.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滄竻闄ょ郴缁熲€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProClearSystemIcon = Load("Resources/Image/TwrpPro/professional_clear_system.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滄牸寮忓寲 Data鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProFormatDataIcon = Load("Resources/Image/TwrpPro/professional_format_data.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滆繘鍏ヤ晶杞解€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProSideloadIcon = Load("Resources/Image/TwrpPro/professional_sideload.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滃埛鍏ユ枃浠垛€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProFlashFileIcon = Load("Resources/Image/TwrpPro/professional_flash_file.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滄洿鏂板唴鏍糕€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProUpdateKernelIcon = Load("Resources/Image/TwrpPro/professional_update_kernel.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滄洿鏂?TWRP鈥濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProUpdateTwrpIcon = Load("Resources/Image/TwrpPro/professional_update_twrp.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滆В瀵嗏€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProDecryptIcon = Load("Resources/Image/TwrpPro/professional_decrypt.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滃叧闂紑鍙戣€呪€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProDisableDeveloperIcon = Load("Resources/Image/TwrpPro/professional_disable_developer.png");

        /// <summary>
        /// TWRP 涓撲笟鎸囦护鈥滆烦杩囧悜瀵尖€濆浘鏍囥€?        /// </summary>
        public static readonly ImageSource TwrpProSkipGuideIcon = Load("Resources/Image/TwrpPro/professional_skip_guide.png");
        public static readonly ImageSource TwrpDropDownArrowIcon = Load("Resources/Image/TwrpPro/professional_dropdown_arrow.png");

        /// <summary>
        /// 鍔犺浇鍥炬爣璧勬簮锛屽吋瀹?PNG 鍜?SVG 鏂囨湰鏂囦欢銆?        /// </summary>
        /// <param name="relativePath">鐩稿璧勬簮璺緞銆?/param>
        /// <returns>鍥惧儚璧勬簮銆?/returns>
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
        /// 鎵撳紑 WPF 璧勬簮娴併€?        /// </summary>
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
        /// 璇诲彇娴佷腑鐨勫叏閮ㄥ瓧鑺傘€?        /// </summary>
        private static byte[] ReadAllBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 鍒ゆ柇鏄惁涓?PNG 鏂囦欢銆?        /// </summary>
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
        /// 鍔犺浇 PNG 鍥炬爣銆?        /// </summary>
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
        /// 鍔犺浇 SVG 鏂囨湰骞惰浆鎹负 DrawingImage銆?        /// </summary>
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
        /// 瑙ｆ瀽 SVG 棰滆壊鍊笺€?        /// </summary>
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
        /// 瑙ｆ瀽 SVG 鏁板€笺€?        /// </summary>
        private static double ParseDouble(string valueText, double defaultValue)
        {
            return double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }
    }
}

