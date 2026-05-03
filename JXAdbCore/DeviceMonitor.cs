using JXAdbCore.EventHandler;
using JXAdbCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JXAdbCore
{
    public class DeviceMonitor
    {
        public event EventHandler<DeviceDataEventArgs> DeviceChanged;
        public event EventHandler<DeviceDataEventArgs> DeviceConnected;
        public event EventHandler<DeviceDataEventArgs> DeviceDisconnected;
        private readonly List<DeviceData> devices = new List<DeviceData>();
        public List<DeviceData> Devices { get { return devices; } }
        private AdbSocket Socket;
        private Thread MonitorThread;
        private CancellationTokenSource MonitorCTS;
        public DeviceMonitor(AdbSocket adbSocket)
        {
            this.Socket = adbSocket;
        }

        public void Start() {
            MonitorCTS = new CancellationTokenSource();
            MonitorThread = new Thread(DeviceMonitorLoop);
            MonitorThread.Start();
        }

        private void InitializeSocket()
        {
            Socket.SendAdbRequest("host:track-devices");
            Socket.ReadAdbResponse();
        }

        private void UpdateDevices(List<DeviceData> devices)
        {
            //检查新增或者状态发送改变
            foreach (DeviceData device in devices)
            {
                DeviceData existingDevice = Devices.SingleOrDefault(d => d.Serial == device.Serial);

                if (existingDevice == null)
                {
                    this.devices.Add(device);
                    OnDeviceConnected(new DeviceDataEventArgs(device));
                }
                else if (existingDevice.State != device.State)
                {
                    existingDevice.State = device.State;
                    OnDeviceChanged(new DeviceDataEventArgs(existingDevice));
                }
            }

            //检查断开连接
            foreach (DeviceData device in Devices.Where(d => !devices.Any(e => e.Serial == d.Serial)).ToArray())
            {
                this.devices.Remove(device);
                OnDeviceDisconnected(new DeviceDataEventArgs(device));
            }
        }

        private void ProcessIncomingDeviceData(string result)
        {
            if (result == null) return;

            List<DeviceData> list = new List<DeviceData>();

            string[] deviceValues = result.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<DeviceData> currentDevices = deviceValues.Select(DeviceData.CreateFromAdbData).ToList();
            UpdateDevices(currentDevices);
        }

        private void DeviceMonitorLoop()
        {
            InitializeSocket();
            do
            {
                try
                {
                    if (Socket.Connected == false) break;
                    string value = Socket.ReadString();
                    ProcessIncomingDeviceData(value);
                }
                catch { 
                }
                Thread.Sleep(1);
            }
            while (!MonitorCTS.IsCancellationRequested);
        }

        protected void OnDeviceChanged(DeviceDataEventArgs e) => DeviceChanged?.Invoke(this, e);
        protected void OnDeviceConnected(DeviceDataEventArgs e) => DeviceConnected?.Invoke(this, e);
        protected void OnDeviceDisconnected(DeviceDataEventArgs e) => DeviceDisconnected?.Invoke(this, e);
    }
}
