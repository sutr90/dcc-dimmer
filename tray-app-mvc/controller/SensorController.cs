using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;
using HidSharp.Reports.Input;
using HidSharp.Utility;
using tray_app_mvc.model;

namespace tray_app_mvc.controller
{
    public class SensorController
    {
        private readonly SensorModel _model;

        public SensorController(SensorModel model)
        {
            _model = model;
            DeviceList.Local.Changed += (sender, args) => _model.SetDeviceList();
            HidSharpDiagnostics.EnableTracing = true;
            HidSharpDiagnostics.PerformStrictChecks = true;
        }

        public void OnDeviceListChanged()
        {
            Debug.Print("Device list changed");
            var modelSensorList = _model.SensorList;

            if (modelSensorList.Count == 0)
            {
                Debug.Print("no device available");
                _model.SetValue(-1);
            }
            else
            {
                Debug.Print("selected new device");
                Task.Factory.StartNew(() => OpenDevice(modelSensorList[0]), TaskCreationOptions.LongRunning);
            }
        }

        private void OpenDevice(HidDevice dev)
        {
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
                                    WriteDeviceItemInputParserResult(inputParser);
                                }
                            }
                        };

                        inputReceiver.Stopped += (sender, args) => { _model.SetValue(-1); };
                        inputReceiver.Started += (sender, args) => { Debug.Print("Started receiving"); };

                        inputReceiver.Start(hidStream);

                    }
                }
                else
                {
                    Debug.Print("Failed to open device.");
                    _model.SetValue(-1);
                }
            }
            catch (Exception e)
            {
                _model.SetValue(-1);
                Debug.Print(e.ToString());
            }
        }

        private void WriteDeviceItemInputParserResult(DeviceItemInputParser parser)
        {
            const int exponentMask = 0b1111_0000_0000_0000;
            const int mantissaMask = 0b0000_1111_1111_1111;

            var dataValue = parser.GetValue(0).GetLogicalValue();

            var exp = (dataValue & exponentMask) >> 12;
            var mantissa = dataValue & mantissaMask;
            var lux = 0.01 * Math.Pow(2, exp) * mantissa;

            _model.SetValue(lux);
        }
    }
}