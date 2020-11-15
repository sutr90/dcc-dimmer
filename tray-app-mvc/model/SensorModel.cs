using System;
using System.Collections.Generic;
using System.Linq;
using HidSharp;

namespace tray_app_mvc.model
{
    public class SensorModel
    {
        private const int VendorId = 0x2341;

        private double Value { get; set; }

        public List<HidDevice> SensorList { get; private set; }

        public SensorModel()
        {
            SensorList = new List<HidDevice>();
            SetDeviceList();
        }

        public void SetValue(double value)
        {
            Value = value;
            var args = new SensorValueChangedEventArgs {Value = value};
            SensorValueChanged?.Invoke(args);
        }

        public void SetDeviceList()
        {
            var devices = DeviceList.Local;
            SensorList = devices.GetHidDevices().ToList().FindAll(device => device.VendorID == VendorId);
            DeviceListChanged?.Invoke();
        }

        public event Action<SensorValueChangedEventArgs> SensorValueChanged;

        public event Action DeviceListChanged;
    }

    public class SensorValueChangedEventArgs : EventArgs
    {
        public double Value { get; set; }
    }
}