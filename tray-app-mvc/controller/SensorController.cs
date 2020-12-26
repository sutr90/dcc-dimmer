using System;
using System.Diagnostics;
using System.Threading;
using HidSharp;
using HidSharp.Reports.Input;
using HidSharp.Utility;
using tray_app_mvc.model;

namespace tray_app_mvc.controller
{
    public class SensorController : IController
    {
        private readonly SensorModel _sensorModel;
        private readonly MonitorDisplayController _displayController;

        private readonly EventWaitHandle _waitHandle;

        public SensorController(SensorModel sensorModel, MonitorDisplayController displayController)
        {
            _sensorModel = sensorModel;
            _displayController = displayController;
            DeviceList.Local.Changed += (sender, args) => _sensorModel.SetDeviceList();
            HidSharpDiagnostics.EnableTracing = true;
            HidSharpDiagnostics.PerformStrictChecks = true;

            _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        }

        public void OnDeviceListChanged()
        {
            Debug.Print("Device list changed");
            var modelSensorList = _sensorModel.SensorList;
            _waitHandle.Set();

            if (modelSensorList.Count == 0)
            {
                Debug.Print("no device available");
                _sensorModel.SetValue(-1);
            }
            else
            {
                Debug.Print("selected new device");
                _waitHandle.Reset();
                var t = new Thread(() => OpenDevice(modelSensorList[0], _waitHandle));
                t.Start();
            }
        }

        private void OpenDevice(HidDevice dev, WaitHandle eventWaitHandle)
        {
            Debug.Print("Sensor thread started");
            try
            {
                var reportDescriptor = dev.GetReportDescriptor();

                // Lengths should match.
                Debug.Assert(dev.GetMaxInputReportLength() == reportDescriptor.MaxInputReportLength);
                Debug.Assert(dev.GetMaxOutputReportLength() == reportDescriptor.MaxOutputReportLength);
                Debug.Assert(dev.GetMaxFeatureReportLength() == reportDescriptor.MaxFeatureReportLength);

                var deviceItem = reportDescriptor.DeviceItems[0];
                if (dev.TryOpen(out var hidStream))
                {
                    hidStream.ReadTimeout = Timeout.Infinite;

                    using (hidStream)
                    {
                        var inputReportBuffer = new byte[dev.GetMaxInputReportLength()];
                        var inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                        var inputParser = deviceItem.CreateDeviceItemInputParser();

                        inputReceiver.Received += (sender, e) =>
                        {
                            while (inputReceiver.TryRead(inputReportBuffer, 0, out var report))
                            {
                                if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                {
                                    GetSensorReadout(inputParser);
                                }
                            }
                        };

                        inputReceiver.Stopped += (sender, args) => { _sensorModel.SetValue(-1); };
                        inputReceiver.Started += (sender, args) => { Debug.Print("Started receiving"); };

                        inputReceiver.Start(hidStream);
                        eventWaitHandle.WaitOne();
                    }
                }
                else
                {
                    Debug.Print("Failed to open device.");
                    _sensorModel.SetValue(-1);
                }
            }
            catch (Exception e)
            {
                _sensorModel.SetValue(-1);
                Debug.Print(e.ToString());
            }

            Debug.Print("Sensor thread ended");
        }

        private void GetSensorReadout(DeviceItemInputParser parser)
        {
            const int exponentMask = 0b1111_0000_0000_0000;
            const int mantissaMask = 0b0000_1111_1111_1111;

            var dataValue = parser.GetValue(0).GetLogicalValue();

            var exp = (dataValue & exponentMask) >> 12;
            var mantissa = dataValue & mantissaMask;
            var lux = 0.01 * Math.Pow(2, exp) * mantissa;

            _sensorModel.SetValue(lux);

            if (!_sensorModel.ManualMode)
            {
                _displayController.ChangeDisplayBrightness(TransferFunction(lux));
            }
        }

        private static int TransferFunction(double lux)
        {
            // calculated value
            // y = 2.669 * (x ^ 0,4995)
            // ~ y = 2.669 * sqrt(x)
            return Math.Clamp((int) Math.Floor(2.669 * Math.Sqrt(lux)), 0, 100);
        }

        public void OnShutdown()
        {
            _waitHandle.Set();
        }

        public void OnManualModeChanged(bool obj)
        {
            _sensorModel.SetManualMode(obj);
        }
    }
}