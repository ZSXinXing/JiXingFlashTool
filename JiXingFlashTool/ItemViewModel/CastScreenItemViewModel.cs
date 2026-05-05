using CommunityToolkit.Mvvm.ComponentModel;
using JiXingFlashTool.Model;
using JiXingFlashTool.Services;
using JiXingFlashTool.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace JiXingFlashTool.ItemViewModel
{
    public class CastScreenItemViewModel : DeviceItemViewModel
    {
        private CastScreenService castScreenService;
        public CastScreenService CastScreenService { get { return castScreenService; } }

        private int index;

        public int Index
        {
            get => index;
            set => SetProperty(ref index, value);
        }

        private bool isControl = false;
        public bool IsControl
        {
            get => isControl;
            set => SetProperty(ref isControl, value);
        }

        private void OnD3DChanage(object sender, EventArg.ScreenD3DEventArgs e)
        {
            OnPropertyChanged("D3DImageSource");
        }


        public D3DImageSource D3DImageSource
        {
            get
            {
                if (castScreenService == null) return null;
                return castScreenService.D3dIS;
            }
        }

        private double screenWidth;
        public double ScreenWidth
        {
            get { return screenWidth; }
            set => SetProperty(ref screenWidth, value);
        }

        private double screenHeight;
        public double ScreenHeight
        {
            get { return screenHeight; }
            set => SetProperty(ref screenHeight, value);
        }

        public CastScreenItemViewModel(DeviceModel device, CastScreenService castScreenService) : base(device)
        {
            this.device = device;
            this.castScreenService = castScreenService;
            this.castScreenService.D3DChanage += OnD3DChanage;
        }
    }
}

