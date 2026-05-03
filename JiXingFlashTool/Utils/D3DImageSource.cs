using HandyControl.Collections;
using JXAdbCore.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using System.Windows;
using SharpDX.Direct3D9;
using SharpDX;
using SharpDX.Mathematics.Interop;

namespace JiXingFlashTool.Utils
{
    public enum FrameFormat
    {
        YV12 = 0,
        NV12 = 1,
        YUY2 = 2,
        UYVY = 3,
        RGB15 = 10,
        RGB16 = 11,
        RGB24 = 12,
        RGB32 = 13,
        ARGB32 = 14
    }
    public class D3DImageSource : D3DImage, IDisposable
    {
        static Format D3DFormatYV12 = D3DX.MakeFourCC((byte)'Y', (byte)'V', (byte)'1', (byte)'2');
        static Format D3DFormatNV12 = D3DX.MakeFourCC((byte)'N', (byte)'V', (byte)'1', (byte)'2');
        static RawColorBGRA BlackColor = new RawColorBGRA(0, 0, 0, 0xFF);
        Int32Rect imageSourceRect;
        Direct3D direct3D;
        int adapterId;
        CreateFlags createFlag;
        Device device;
        Surface inputSurface;
        Texture texture;
        Surface textureSurface;
        DisplayMode displayMode;
        FrameFormat frameFormat;
        int _width;
        int _height;
        bool isVistaOrBetter;
        private bool isDisposed = false;
        public D3DImageSource(int videoWidth, int videoHeight, FrameFormat format, int adapterId = 0)
        {

            try
            {
                isVistaOrBetter = IsVistaOrBetter;
                IsFrontBufferAvailableChanged += this.OnIsFrontBufferAvailableChanged;
                InitD3D(adapterId);
                Format d3dFormat = ConvertToD3D(format);
                if (!this.CheckFormat(d3dFormat))
                {
                    // 显卡不支持该格式
                    throw new FormatException("显卡不支持该格式:" + format);
                }
                Capabilities deviceCap = this.direct3D.GetDeviceCaps(this.adapterId, DeviceType.Hardware);
                if (deviceCap.MaxTextureWidth < videoWidth || deviceCap.MaxTextureHeight < videoHeight)
                {
                    throw new Exception("显卡不支持分辨率:" + videoWidth + "*" + videoHeight);
                }
                this.frameFormat = format;
                _width = videoWidth;
                _height = videoHeight;
                CreateResource(d3dFormat, videoWidth, videoHeight);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
        public D3DImageSource()
        {
        }

        /// <summary>
        /// 渲染
        /// </summary>
        /// <param name="data">数据，比如YU12放在data[0-2]、NV12放在data[0-1]、ARGB32放在data[0],具体参考ffmpeg frame.data</param>
        /// <param name="lineSize">linesize表示每行数据长度。具体参考ffmpeg frame.lineSize</param>
        [HandleProcessCorruptedStateExceptions]
        public void Present(IntPtr[] data, int[] lineSize)
        {
            if (this.isDisposed) return;

            FillBuffer(data, lineSize);
            StretchSurface();
            InvalidateImage();
        }
        /// <summary>
        /// 清除画面
        /// </summary>
        public void Clear()
        {
            try
            {
                this.device.ColorFill(this.textureSurface, BlackColor);
            }
            catch
            {
            }
        }

        bool CheckFormat(FrameFormat format)
        {
            return this.CheckFormat(ConvertToD3D(format));
        }
        void InitD3D(int adapterId = 0)
        {
            this.adapterId = adapterId;
            this.direct3D = isVistaOrBetter ? new Direct3DEx() : new Direct3D();
            this.displayMode = this.direct3D.GetAdapterDisplayMode(this.adapterId);
            Capabilities deviceCap = this.direct3D.GetDeviceCaps(this.adapterId, DeviceType.Hardware);
            this.createFlag = CreateFlags.Multithreaded;
            if ((int)deviceCap.VertexProcessingCaps != 0)
            {
                this.createFlag |= CreateFlags.HardwareVertexProcessing;
            }
            else
            {
                this.createFlag |= CreateFlags.SoftwareVertexProcessing;
            }
        }
        void CreateResource(Format format, int width, int height)
        {
            PresentParameters presentParameters = this.GetPresentParameters(width, height);
            this.device = isVistaOrBetter ?
            new DeviceEx((Direct3DEx)this.direct3D, this.adapterId, DeviceType.Hardware, IntPtr.Zero, this.createFlag, presentParameters) :
              new Device(this.direct3D, this.adapterId, DeviceType.Hardware, IntPtr.Zero, this.createFlag, presentParameters);
            this.texture = new Texture(this.device, width, height, 1, Usage.RenderTarget, this.displayMode.Format, Pool.Default);
            this.textureSurface = this.texture.GetSurfaceLevel(0);
            this.inputSurface = isVistaOrBetter ?
                Surface.CreateOffscreenPlainEx((DeviceEx)this.device, width, height, format, Pool.Default, Usage.None) :
                Surface.CreateOffscreenPlain(this.device, width, height, format, Pool.Default);
            this.device.ColorFill(this.inputSurface, BlackColor);
            this.SetImageSourceBackBuffer();
        }

        PresentParameters GetPresentParameters(int width, int height)
        {
            PresentParameters presentParams = new PresentParameters();
            presentParams.PresentFlags = PresentFlags.Video | PresentFlags.OverlayYCbCrBt709;
            presentParams.Windowed = true;
            presentParams.DeviceWindowHandle = IntPtr.Zero;
            presentParams.BackBufferWidth = width == 0 ? 1 : width;
            presentParams.BackBufferHeight = height == 0 ? 1 : height;
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.PresentationInterval = PresentInterval.Immediate;
            presentParams.BackBufferFormat = this.displayMode.Format;
            presentParams.BackBufferCount = 1;
            presentParams.EnableAutoDepthStencil = false;
            return presentParams;
        }

        [HandleProcessCorruptedStateExceptions]
        void FillBuffer(IntPtr[] data, int[] lineSize)
        {
            try
            {
                DataRectangle rect = this.inputSurface.LockRectangle(LockFlags.None);
                IntPtr surfaceBufferPtr = rect.DataPointer;
                switch (this.frameFormat)
                {
                    case FrameFormat.YV12:
                        {
                            if (lineSize[0] == rect.Pitch)
                            {
                                int ySize = lineSize[0] * (int)_height;
                                int uSize = lineSize[1] * (int)_height / 2;
                                int vSize = lineSize[2] * (int)_height / 2;
                                Memcpy(surfaceBufferPtr, data[0], ySize);
                                surfaceBufferPtr += ySize;
                                Memcpy(surfaceBufferPtr, data[1], uSize);
                                surfaceBufferPtr += uSize;
                                Memcpy(surfaceBufferPtr, data[2], vSize);
                            }
                            else
                            {
                                int dataSize = rect.Pitch < lineSize[0] ? rect.Pitch : lineSize[0];
                                for (int i = 0; i < _height; i++)
                                {
                                    Memcpy(surfaceBufferPtr, data[0] + i * lineSize[0], dataSize);
                                    surfaceBufferPtr += rect.Pitch;
                                }
                                dataSize = rect.Pitch < lineSize[1] ? rect.Pitch : lineSize[1];
                                for (int i = 0; i < _height / 2; i++)
                                {
                                    Memcpy(surfaceBufferPtr, data[1] + i * lineSize[1], dataSize);
                                    surfaceBufferPtr += rect.Pitch / 2;
                                }

                                dataSize = rect.Pitch < lineSize[2] ? rect.Pitch : lineSize[2];
                                for (int i = 0; i < _height / 2; i++)
                                {
                                    Memcpy(surfaceBufferPtr, data[2] + i * lineSize[2], dataSize);
                                    surfaceBufferPtr += rect.Pitch / 2;
                                }
                            }
                        }
                        break;
                    case FrameFormat.NV12:
                        {

                            if (lineSize[0] == rect.Pitch)
                            {
                                int ySize = lineSize[0] * (int)_height;
                                int uvSize = lineSize[1] * (int)_height / 2;
                                Memcpy(surfaceBufferPtr, data[0], ySize);
                                surfaceBufferPtr += ySize;
                                Memcpy(surfaceBufferPtr, data[1], uvSize);
                            }
                            else
                            {
                                int dataSize = rect.Pitch < lineSize[0] ? rect.Pitch : lineSize[0];
                                for (int i = 0; i < _height; i++)
                                {
                                    Memcpy(surfaceBufferPtr, data[0] + i * lineSize[0], dataSize);
                                    surfaceBufferPtr += rect.Pitch;
                                }

                                dataSize = rect.Pitch < lineSize[1] ? rect.Pitch : lineSize[1];
                                for (int i = 0; i < _height / 2; i++)
                                {
                                    Memcpy(surfaceBufferPtr, data[1] + i * lineSize[1], dataSize);
                                    surfaceBufferPtr += rect.Pitch;
                                }
                            }
                        }
                        break;
                    case FrameFormat.YUY2:
                    case FrameFormat.UYVY:
                    case FrameFormat.RGB15:
                    case FrameFormat.RGB16:
                    case FrameFormat.RGB24:
                    case FrameFormat.RGB32:
                    case FrameFormat.ARGB32:
                    default:
                        if (lineSize[0] == rect.Pitch)
                        {
                            Memcpy(surfaceBufferPtr, data[0], lineSize[0] * (int)_height);
                        }
                        else
                        {
                            int dataSize = rect.Pitch < lineSize[0] ? rect.Pitch : lineSize[0];
                            for (int i = 0; i < _height; i++)
                            {
                                Memcpy(surfaceBufferPtr, data[0] + i * lineSize[0], dataSize);
                                surfaceBufferPtr += rect.Pitch;
                            }
                        }
                        break;
                }

                if (this.inputSurface != null) this.inputSurface.UnlockRectangle();
            }
            catch
            {
            }
        }
        void StretchSurface()
        {
            try
            {
                this.device.StretchRectangle(this.inputSurface, this.textureSurface, TextureFilter.Linear);
            }
            catch
            {
            }
        }
        /// <summary>
        /// 检查d3d设备是否正常
        /// </summary>
        /// <returns></returns>
        bool CheckDevice()
        {
            if (isVistaOrBetter)
            {
                SharpDX.Direct3D9.DeviceState state = ((DeviceEx)this.device).CheckDeviceState(IntPtr.Zero);
                return state == SharpDX.Direct3D9.DeviceState.Ok;
            }
            else
            {
                return false; // xp无法支持ex
            }
        }
        void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (IsFrontBufferAvailable && this.textureSurface != null)
                {
                    Lock();
                    SetBackBuffer(D3DResourceType.IDirect3DSurface9, this.textureSurface.NativePointer);
                    Unlock();
                }
            }
            catch
            {
            }
        }
        void SetImageSourceBackBuffer()
        {

            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke((Action)(() => this.SetImageSourceBackBuffer()));
                    return;
                }

                Lock();
                SetBackBuffer(D3DResourceType.IDirect3DSurface9, this.textureSurface.NativePointer);
                Unlock();
                this.imageSourceRect = new Int32Rect(0, 0, PixelWidth, PixelHeight);
            }
            catch
            {
            }

        }
        void InvalidateImage()
        {
            try
            {
                if (!IsFrontBufferAvailable)
                {
                    return;
                }

                Lock();
                AddDirtyRect(this.imageSourceRect);
                Unlock();
            }
            catch
            {
            }

        }
        bool CheckFormat(Format d3dFormat)
        {
            if (!this.direct3D.CheckDeviceFormat(this.adapterId, DeviceType.Hardware, this.displayMode.Format, Usage.None, SharpDX.Direct3D9.ResourceType.Surface, d3dFormat))
            {
                return false;
            }
            return this.direct3D.CheckDeviceFormatConversion(this.adapterId, DeviceType.Hardware, d3dFormat, this.displayMode.Format);
        }
        static Format ConvertToD3D(FrameFormat format)
        {
            switch (format)
            {
                case FrameFormat.YV12:
                    return D3DFormatYV12;
                case FrameFormat.NV12:
                    return D3DFormatNV12;
                case FrameFormat.YUY2:
                    return Format.Yuy2;
                case FrameFormat.UYVY:
                    return Format.Uyvy;
                case FrameFormat.RGB15:
                    return Format.X1R5G5B5;
                case FrameFormat.RGB16:
                    return Format.R5G6B5;
                case FrameFormat.RGB32:
                    return Format.X8R8G8B8;
                case FrameFormat.ARGB32:
                    return Format.A8R8G8B8;
                case FrameFormat.RGB24:
                    return Format.R8G8B8;
                default:
                    throw new ArgumentException("Unknown pixel format", "format");
            }
        }
        static int GetArgb(byte a, byte r, byte g, byte b)
        {
            return a << 24 + r << 16 + g << 8 + b;
        }

        static bool IsVistaOrBetter
        {
            get
            {
                return Environment.OSVersion.Version.Major >= 6;
            }
        }


        public void Dispose()
        {
            this.Dispose(true);
            GC.Collect();
        }

        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;

                if (disposing)
                {
                    this.Clear();

                    if (this.inputSurface != null)
                    {
                        this.inputSurface.Dispose();
                        this.inputSurface = null;
                    }

                    if (this.texture != null)
                    {
                        this.texture.Dispose();
                        this.texture = null;
                    }

                    if (this.textureSurface != null)
                    {
                        this.textureSurface.Dispose();
                        this.textureSurface = null;
                    }

                    if (this.device != null)
                    {
                        this.device.Dispose();
                        this.device = null;
                    }

                    if (this.direct3D != null)
                    {
                        this.direct3D.Dispose();
                        this.direct3D = null;
                    }
                }
            }

        }

        [DllImport("ntdll.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr Memcpy(IntPtr dest, IntPtr source, int length);

    }
}
