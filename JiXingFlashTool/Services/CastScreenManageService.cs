using JiXingFlashTool.EventArg;
using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JiXingFlashTool.Services
{
    public class CastScreenManageService
    {
        public event EventHandler<CastScreenManageResultEventArgs> CastScreenManageResult;

        private List<DeviceModel> castScreenTaskList = new List<DeviceModel>();
        private CancellationTokenSource castScreenMonitorCTS;
        private Thread castScreenMonitorThread;
        #region 单例
        private CastScreenManageService()
        {
        }

        public static CastScreenManageService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly CastScreenManageService instance = new CastScreenManageService();
        }
        #endregion

        public void StartMonitor()
        {
            castScreenMonitorCTS = new CancellationTokenSource();
            castScreenMonitorThread = new Thread(CastScreenTask);
            castScreenMonitorThread.IsBackground = true;
            castScreenMonitorThread.Start();
        }

        public void StopMonitor()
        {
            if (castScreenMonitorCTS != null) castScreenMonitorCTS.Cancel();
            if (castScreenMonitorThread != null) castScreenMonitorThread.Abort();
        }

        public void AddCastScreen(DeviceModel device)
        {
            castScreenTaskList.Add(device);
        }

        private void CastScreenTask()
        {
            while (!castScreenMonitorCTS.Token.IsCancellationRequested)
            {

                if (castScreenTaskList == null || castScreenTaskList.Count == 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                DeviceModel device = castScreenTaskList.First();
                castScreenTaskList.Remove(device);
                if (device == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

                CastScreenService service = new CastScreenService(device);
                service.Start(AppService.Instance.AppConfig.MiniCastScreenResolution, 1, AppService.Instance.AppConfig.MiniCastScreenRate);
                service.ScreenResult += OnScreenResult;
                Thread.Sleep(1);
            };
        }

        private void OnScreenResult(object sender, EventArg.CastScreenResultEventArgs e)
        {
            if (e.Result == false)
            {
                AddCastScreen(e.Device);
            }
            else
            {
                //连接成功
                Debug.WriteLine($"IP:{e.Device.Serial} 投屏成功CastScreenManageService");
                if (CastScreenManageResult != null)
                {
                    CastScreenManageResultEventArgs eventArgs = CastScreenManageResultEventArgs.NewFormSuper(e, (CastScreenService)sender);
                    CastScreenManageResult(this, eventArgs);
                }
            }
        }
    }
}
