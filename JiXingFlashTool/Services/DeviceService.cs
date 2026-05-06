using HandyControl.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using JiXingFlashTool.Extensions;
using JXAdbCore.Enums;
using JXAdbCore;
using JiXingFlashTool.Model;
using JiXingFlashTool.EventArg;
using JiXingFlashTool.Utils;
using HandyControl.Controls;

namespace JiXingFlashTool.Services
{
    public class DeviceService
    {

        #region 属性
        private List<DeviceModel> deviceList = new List<DeviceModel>();
        public List<DeviceModel> DeviceList { get { return deviceList; } }
        public event EventHandler<DeviceEventArgs> DeviceConnected;
        public event EventHandler<DeviceEventArgs> DeviceDisconnected;
        public event EventHandler<DeviceEventArgs> DeviceChange;
        public event EventHandler<DeviceEventArgs> DeviceRemarkChange;
        public event EventHandler<DeviceEventArgs> DeviceEnvironmentChange;
        public List<EthernetSNModel> EthernetSNList { get; set; }
        public event EventHandler<EthernetSNEventArgs> EthernetSNChanage;
        private Thread SNDeviceThread;
        private CancellationTokenSource SNDeviceCTS;
        private DeviceMonitor monitor;

        private object o = new object();
        #endregion

        #region 单例
        private DeviceService()
        {

        }
        public static DeviceService Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested()
            {
            }
            internal static readonly DeviceService instance = new DeviceService();
        }

        public void Init() {
            EthernetSNModel ethernetSN = CommonTool.GetPCIPV4SNModel();
            List<EthernetSNModel> list = new List<EthernetSNModel>();
            list.Add(ethernetSN);
            EthernetSNList = list;

        }

        #endregion

        #region 方法
        public Task<int> ClearEthernetDevice() {
            Task<int> task = new Task<int>(() =>
            {
                int successCount = 0;
                List<DeviceModel> list = AdbService.Instance.Devices();
                for (int i = 0; i < list.Count; i++)
                {
                    DeviceModel device = list[i];
                    string[] serialArray = device.Serial.Split(':');
                    if (serialArray.First().IsIp()) {
                        DnsEndPoint dnsEndPoint = new DnsEndPoint(serialArray[0], int.Parse(serialArray[1]));
                        if (AdbService.Instance.Disconnect(dnsEndPoint))
                        {
                            successCount++;
                        }
                    }
                }
                return successCount;
            });
            task.Start();
            return task;
        }

        public void SNEthernetDevice() {
            List<EthernetSNModel> ethernetSNModels = this.EthernetSNList;

            foreach (EthernetSNModel ethernetSN in ethernetSNModels)
            {

                int snCount = 0;
                int connectCount = 0;

                //切割好数组
                string[] startIP = ethernetSN.StartIP.Split('.');
                string[] endIP = ethernetSN.EndIP.Split('.');

                //获取开始IP与结束IP
                int startIPNumber = int.Parse(ethernetSN.StartIP.Split('.')[3]);
                int endIPNumber = int.Parse(ethernetSN.EndIP.Split('.')[3]);

                for (int i = startIPNumber; i <= endIPNumber; i++)
                {

                    //组建新的IP进行扫描
                    string ipString = string.Format("{0}.{1}.{2}.{3}", startIP[0], startIP[1], startIP[2], i);

                    bool existDevice = false;
                    System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient();
                    IPAddress ip = IPAddress.Parse(ipString);
                    try
                    {
                        var result = tcpClient.BeginConnect(ip, int.Parse(ethernetSN.Port), null, null);
                        existDevice = result.AsyncWaitHandle.WaitOne(int.Parse(ethernetSN.Timeout));
                    }
                    catch (Exception ex)
                    {
                    }
                    finally {
                        if (tcpClient != null) {
                            tcpClient.Close();
                            tcpClient.Dispose();
                        }
                    }

                    //如果能连接说明设备存在
                    if (existDevice)
                    {
                        snCount++;
                        ethernetSN.SNCount = snCount;
                        //下发通知改变
                        if (EthernetSNChanage != null)
                        {
                            EthernetSNChanage(this, new EthernetSNEventArgs(ethernetSN));
                        }

                        //实现连接
                        DnsEndPoint dnsEndPoint = new DnsEndPoint(ipString, int.Parse(ethernetSN.Port));
                        bool connectRet = false;
                        try
                        {
                            connectRet = AdbService.Instance.Connect(dnsEndPoint);
                        }
                        catch {
                        }
                        if (connectRet == true)
                        {
                            connectCount++;
                            ethernetSN.ConnectCount = connectCount;
                            if (EthernetSNChanage != null)
                            {
                                EthernetSNChanage(this, new EthernetSNEventArgs(ethernetSN));
                            }
                        }
                    }
                    Thread.Sleep(1);
                }

                Thread.Sleep(1);
            }
        }

        private void SNDeviceTask() {
            while (!SNDeviceCTS.IsCancellationRequested)
            {
                try
                {
                    SNEthernetDevice();
                }
                catch { 
                }
                Thread.Sleep(5 * 1000);
            }
        }


        private void AddDevice(DeviceModel newDevice) {

            //由于特殊性这里休眠1000
            Thread.Sleep(100);
            List<DeviceModel> list = AdbService.Instance.Devices();

            DeviceModel device = list.Find(it => {
                if (it == null) return false;
                return it.Serial == newDevice.Serial;
            });

            if (device == null || device.State == JXAdbCore.Enums.DeviceState.Offline || CheckExistDevice(device)) return;

            Debug.WriteLine(string.Format("DeviceService 接受新的设备:{0} 状态:{1}", device.RoSerialNo, device.State));

            lock (o) {
                ThreadPool.QueueUserWorkItem((x) =>
                {
                    DeviceEventArgs deviceEventArgs = new DeviceEventArgs(device);
                    //获取root类型
                    try
                    {
                        //判断是否存在adb root
                        if (AdbService.Instance.ExecuteRemoteCommand("ls /cache/recovery/", device).IndexOf("Permission denied") == -1)
                        {
                            device.RootType = Enums.SystemRootType.Shell;
                        }
                        //判断是否存在kernelsu
                        else if (AdbService.Instance.ExecuteRemoteCommand("ls -l /system/bin/su", device).IndexOf("No such file or directory") == -1)
                        {
                            device.RootType = Enums.SystemRootType.Kernelsu;
                        }
                        //不存在
                        else
                        {
                            device.RootType = Enums.SystemRootType.None;
                        }
                    }
                    catch
                    {
                        device.RootType = Enums.SystemRootType.None;
                    }

                    try
                    {
                        if (device.Model.RetailModel() != string.Empty)
                        {
                            device.RetailModel = device.Model.RetailModel();
                        }
                        device.Brand = AdbService.Instance.GetProp(device, "ro.product.brand").ReplaceText("\r\n", "");
                        device.RoSerialNo = AdbService.Instance.GetRoSerialno(device).ReplaceText("\r\n", "");
                        device.ScreenSize = AdbService.Instance.GetScreenSize(device);
                        device.BuildDate = AdbService.Instance.GetBuildDate(device);

                        //如果是在系统就获取版本
                        if (device.State == DeviceState.Online)
                        {
                            //获取系统版本
                            device.AndroidVersion = AdbService.Instance.GetAndroidVersion(device).ReplaceText("\r\n", "");
                            //获取内部版本
                            if (int.Parse(device.AndroidVersion) >= 13)
                            {
                                device.PolestarVersion = AdbService.Instance.ExecuteRemoteCommand("su -c getprop ro.polestar.system.version",device).ReplaceText("\r\n", "");
                            }
                        }
                        else if (device.State == DeviceState.Recovery) {
                            //获取TWRP版本
                            device.TWRPVersion = AdbService.Instance.GetProp(device, "ro.twrp.version").ReplaceText("\r\n", "");
                        }
                    }
                    catch (Exception ex)
                    {
                    }


                    if (CheckExistDevice(device)) return;

                    //进行排序
                    int insertIndex = deviceList.Count;

                    try
                    {

                        if (CheckExistDevice(device)) return;
                        Debug.WriteLine(string.Format("Device：{0} 插入位置:{1}", device.Serial, insertIndex));
                        //添加到队列
                        deviceList.Insert(insertIndex, device);
                        deviceEventArgs.Index = insertIndex;

                        if (DeviceConnected != null) DeviceConnected(this, deviceEventArgs);
                    }
                    catch { 
                    }
                });
            }

        }

        private void RemoveDevice(DeviceModel removeDevice) {
            DeviceModel device = deviceList.Find(it =>
            {
                if (it == null) return false;
                return it.Serial.Equals(removeDevice.Serial);
            });

            DeviceEventArgs deviceEventArgs = new DeviceEventArgs(device);

            if (DeviceDisconnected != null) DeviceDisconnected(this, deviceEventArgs);

            deviceList.Remove(device);

        }


        public void StartDeviceMonitor(DeviceMonitor monitor) {

            this.monitor = monitor;
            if (monitor != null) {
                monitor.DeviceDisconnected += OnDeviceDisconnected;
                monitor.DeviceConnected += OnDeviceConnected;
                monitor.DeviceChanged += OnDeviceChanged;
                monitor.Start();

                StartListenDevice();
            }
        }

        private void OnDeviceChanged(object sender, JXAdbCore.EventHandler.DeviceDataEventArgs e)
        {
            try
            {
                if (e == null || e.Device == null) return;

                DeviceModel device = new DeviceModel(e.Device);

                DeviceEventArgs deviceEventArgs = new DeviceEventArgs(device);

                if (DeviceChange != null) DeviceChange(this, deviceEventArgs);

                if (device.State == JXAdbCore.Enums.DeviceState.Offline)
                {
                    RemoveDevice(device);
                }
                else if (device.State == JXAdbCore.Enums.DeviceState.Online)
                {
                    AddDevice(device);
                }
            }
            catch
            {
            }
        }

        private void OnDeviceConnected(object sender, JXAdbCore.EventHandler.DeviceDataEventArgs e)
        {
            if (e == null || e.Device == null) return;

            AddDevice(new DeviceModel(e.Device));
        }

        private void OnDeviceDisconnected(object sender, JXAdbCore.EventHandler.DeviceDataEventArgs e)
        {
            if (e == null || e.Device == null) return;
            RemoveDevice(new DeviceModel(e.Device));
        }

        public void AddEthernetSN(EthernetSNModel ethernetSN) {
            try
            {
                SQLService.Instance.Conn.Insert(ethernetSN);
                EthernetSNList.Add(ethernetSN);
            }
            catch { 
            }
        }

        public void RemoveEthernetSN(EthernetSNModel ethernetSN)
        {
            try
            {
                SQLService.Instance.Conn.Delete(ethernetSN);
                EthernetSNList.Remove(ethernetSN);
            }
            catch { 
            }
        }

        private void StartListenDevice() {
            SNDeviceCTS = new CancellationTokenSource();
            SNDeviceThread = new Thread(SNDeviceTask);
            SNDeviceCTS = new CancellationTokenSource();
            SNDeviceThread.Start();
        }

        private void StopListenDevice()
        {
            if (SNDeviceCTS != null) SNDeviceCTS.Cancel();
            if (SNDeviceThread != null) SNDeviceThread.Abort();
        }

        private bool CheckExistDevice(DeviceModel device) {
            return deviceList.Exists((it) =>
            {
                if (it == null) return false;
                return it.Serial.Equals(device.Serial);
            });
        }

        #endregion
    }
}
