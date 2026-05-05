using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using JiXingFlashTool.Delegate;
using JiXingFlashTool.Enums;
using JiXingFlashTool.Extensions;
using JiXingFlashTool.Model;
using JiXingFlashTool.Models;
using JiXingFlashTool.ItemViewModel;
using JiXingFlashTool.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace JiXingFlashTool.ViewModels
{
    /// <summary>
    /// TWRP 涓撲笟鎸囦护寮圭獥瑙嗗浘妯″瀷锛岃礋璐ｇ粍缁囨寚浠ら」銆佹枃浠堕€夋嫨鍜屾墽琛屽洖璋冦€?    /// </summary>
    public class CommandListViewModel : ObservableObject
    {
        /// <summary>
        /// 褰撳墠寮圭獥鎵€閫夎澶囧垪琛ㄣ€?        /// </summary>
        public List<DeviceItemViewModel> SelectDeviceList { get; set; } = new List<DeviceItemViewModel>();

        private readonly ObservableCollection<CommandModel> _commandCollection = new ObservableCollection<CommandModel>();

        /// <summary>
        /// 涓撲笟鎸囦护闆嗗悎銆?        /// </summary>
        public ObservableCollection<CommandModel> CommandCollection => _commandCollection;

        /// <summary>
        /// 鎵ц褰撳墠鎸夐挳瀵瑰簲鐨?TWRP 鎸囦护銆?        /// </summary>
        public RelayCommand<CommandModel> ExecuteCommand => new Lazy<RelayCommand<CommandModel>>(() => new RelayCommand<CommandModel>(Execute)).Value;

        /// <summary>
        /// 鎸囦护鎵ц瀹屾垚鍚庣殑鍥炶皟銆?        /// </summary>
        public CommonFinishDelegate FinishDelegate { get; set; }

        /// <summary>
        /// 褰撳墠寮圭獥瀹炰緥銆?        /// </summary>
        public Dialog Dialog { get; set; }

        /// <summary>
        /// 鍒濆鍖栦笓涓氭寚浠ゅ脊绐楁ā鍨嬨€?        /// </summary>
        public CommandListViewModel()
        {
            InitCommand();
        }

        /// <summary>
        /// 鍒濆鍖?Figma 瀵瑰簲鐨?TWRP 涓撲笟鎸囦护鎸夐挳銆?        /// </summary>
        private void InitCommand()
        {
            _commandCollection.Clear();
            _commandCollection.Add(CreateCommand("重启到系统", TWRPCommandType.RebootSystem, 91, "#5C82FD", "professional_reboot.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("启动到TWRP", TWRPCommandType.RebootTWRP, 102.297, "#7B9EFF", "professional_reboot.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("重启到Download", TWRPCommandType.RebootDownload, 124.031, "#9C27B0", "professional_reboot.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("鍙屾竻", TWRPCommandType.Wipe, 59.5, "#FF9800", "professional_wipe.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("娓呴櫎绯荤粺", TWRPCommandType.ClearSystem, 80.5, "#F44336", "professional_clear_system.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("格式化Data", TWRPCommandType.Format, 95.781, "#F44336", "professional_format_data.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("杩涘叆渚ц浇", TWRPCommandType.Sideload, 80.5, "#7B9EFF", "professional_sideload.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("鍒峰叆鏂囦欢", TWRPCommandType.FlashFile, 80.5, "#FF9800", "professional_flash_file.png", new Thickness(0, 4.25, 0, 0), true));
            _commandCollection.Add(CreateCommand("鏇存柊鍐呮牳", TWRPCommandType.FlashKernel, 80.5, "#5C82FD", "professional_update_kernel.png", new Thickness(0, 4.25, 0, 0), true));
            _commandCollection.Add(CreateCommand("鏇存柊 TWRP", TWRPCommandType.UpdateTWRP, 91.797, "#7B9EFF", "professional_update_twrp.png", new Thickness(0, 4.25, 0, 0), true));
            _commandCollection.Add(CreateCommand("瑙ｅ瘑", TWRPCommandType.Decryption, 59.5, "#FF9800", "professional_decrypt.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("关闭开发者", TWRPCommandType.DisableDeveloper, 91, "#FF9800", "professional_disable_developer.png", new Thickness(0, 4.25, 0, 0)));
            _commandCollection.Add(CreateCommand("璺宠繃鍚戝", TWRPCommandType.SkipGuide, 80.5, "#FF9800", "professional_skip_guide.png", new Thickness(0, 4.25, 0, 0)));
        }

        /// <summary>
        /// 鍒涘缓涓撲笟妯″紡鎸夐挳妯″瀷銆?        /// </summary>
        /// <param name="name">鎸夐挳鍚嶇О銆?/param>
        /// <param name="type">TWRP 鎸囦护绫诲瀷銆?/param>
        /// <param name="width">鎸夐挳瀹藉害銆?/param>
        /// <param name="color">鑳屾櫙鑹层€?/param>
        /// <param name="iconName">鍥炬爣鏂囦欢鍚嶃€?/param>
        /// <param name="textMargin">鏂囧瓧杈硅窛銆?/param>
        /// <param name="needSelectFile">鏄惁闇€瑕侀€夋嫨鏂囦欢銆?/param>
        /// <returns>鎸夐挳妯″瀷銆?/returns>
        private static CommandModel CreateCommand(string name, TWRPCommandType type, double width, string color, string iconName, Thickness textMargin, bool needSelectFile = false)
        {
            var model = new CommandModel(name, type, needSelectFile)
            {
                ButtonWidth = width,
                ButtonMargin = new Thickness(0, 0, 7, 0),
                BackgroundBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(color),
                ForegroundBrush = Brushes.White,
                IconSource = LoadIcon(iconName),
                TextMargin = textMargin
            };

            if (type == TWRPCommandType.FlashFile)
            {
                model.FileFilter = "ZIP 鏂囦欢 (*.zip)|*.zip";
            }
            else if (type == TWRPCommandType.FlashKernel || type == TWRPCommandType.UpdateTWRP)
            {
                model.FileFilter = "IMG 鏂囦欢 (*.img)|*.img";
            }

            return model;
        }

        /// <summary>
        /// 璇诲彇 TWRP 涓撲笟鎸囦护鍥炬爣璧勬簮骞惰浆鎹负 WPF 鐭㈤噺鍥俱€?        /// </summary>
        /// <param name="iconName">鍥炬爣鏂囦欢鍚嶃€?/param>
        /// <returns>鐭㈤噺鍥剧墖璧勬簮銆?/returns>
        private static ImageSource LoadIcon(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName))
            {
                return null;
            }

            var filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "Image", "TwrpPro", iconName);
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }

            var svgText = File.ReadAllText(filePath);
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

                var strokeBrush = ParseBrush(pathNode.Attribute("stroke")?.Value) ?? Brushes.White;
                var strokeWidth = ParseDouble(pathNode.Attribute("stroke-width")?.Value, 1.02083);
                var geometry = Geometry.Parse(pathData);
                var pen = new Pen(strokeBrush, strokeWidth)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                drawingGroup.Children.Add(new GeometryDrawing(null, pen, geometry));
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
        /// 灏?SVG 棰滆壊鍊艰浆鎹负 WPF 鐢诲埛銆?        /// </summary>
        /// <param name="brushText">SVG 棰滆壊鏂囨湰銆?/param>
        /// <returns>鍙敤鐢诲埛銆?/returns>
        private static Brush ParseBrush(string brushText)
        {
            if (string.IsNullOrWhiteSpace(brushText) || brushText.Equals("none", StringComparison.OrdinalIgnoreCase) || brushText.StartsWith("var(", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                return (Brush)new BrushConverter().ConvertFromString(brushText);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 瑙ｆ瀽 SVG 鏁板€煎睘鎬с€?        /// </summary>
        /// <param name="valueText">灞炴€ф枃鏈€?/param>
        /// <param name="defaultValue">榛樿鍊笺€?/param>
        /// <returns>瑙ｆ瀽鍚庣殑鏁板€笺€?/returns>
        private static double ParseDouble(string valueText, double defaultValue)
        {
            return double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// 鏄剧ず涓撲笟鎸囦护寮圭獥銆?        /// </summary>
        /// <param name="deviceList">璁惧鍒楄〃銆?/param>
        /// <param name="finishDelegate">瀹屾垚鍥炶皟銆?/param>
        public static void Show(List<DeviceItemViewModel> deviceList, CommonFinishDelegate finishDelegate)
        {
            var vm = new CommandListViewModel
            {
                SelectDeviceList = deviceList,
                FinishDelegate = finishDelegate
            };

            var view = new CommandListView
            {
                DataContext = vm
            };

            vm.Dialog = Dialog.Show(view);
        }

        /// <summary>
        /// 鎵ц閫変腑鐨勪笓涓氭寚浠ゃ€?        /// </summary>
        /// <param name="model">鎸夐挳妯″瀷銆?/param>
        private void Execute(CommandModel model)
        {
            if (model == null)
            {
                return;
            }

            if (model.NeedSelectFile)
            {
                using (var ofd = new System.Windows.Forms.OpenFileDialog())
                {
                    ofd.Filter = model.FileFilter.IsNull() ? "鏂囦欢|*.*" : model.FileFilter;
                    ofd.Multiselect = false;
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        model.FilePath = ofd.FileName;
                        FinishDelegate?.Invoke(model);
                        Dialog?.Close();
                    }
                }
            }
            else
            {
                FinishDelegate?.Invoke(model);
                Dialog?.Close();
            }
        }
    }
}

