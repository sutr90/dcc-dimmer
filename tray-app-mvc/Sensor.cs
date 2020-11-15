using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HidSharp.Reports.Input;
using HidSharp.Utility;

namespace HidSharp.Test
{
    class Program
    {
        // static void WriteDeviceItemInputParserResult(DeviceItemInputParser parser)
        // {
        //     int valueCount = parser.ValueCount;
        //
        //     for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
        //     {
        //         var dataValue = parser.GetValue(valueIndex).GetLogicalValue();
        //
        //         var topMask = 0b1111_0000_0000_0000;
        //         var bottomMask = 0b0000_1111_1111_1111;
        //
        //         var exp = (dataValue & topMask) >> 12;
        //         var mantissa = dataValue & bottomMask;
        //         var lux = 0.01 * Math.Pow(2, exp) * mantissa;
        //
        //         Console.Write(@$"lux: {lux}");
        //     }
        //
        //     Console.WriteLine();
        // }
        //
        // static void Main(string[] args)
        // {
        //     HidSharpDiagnostics.EnableTracing = true;
        //     HidSharpDiagnostics.PerformStrictChecks = true;
        //
        //     var list = DeviceList.Local;
        //     list.Changed += (sender, e) => Console.WriteLine(@"Device list changed.");
        //
        //     var hidDeviceList = list.GetHidDevices().ToArray();
        //
        //     foreach (var dev in hidDeviceList)
        //     {
        //         if (dev.VendorID != 0x2341) continue;
        //
        //         Console.WriteLine(dev);
        //
        //         try
        //         {
        //             var reportDescriptor = dev.GetReportDescriptor();
        //
        //             // Lengths should match.
        //             Debug.Assert(dev.GetMaxInputReportLength() == reportDescriptor.MaxInputReportLength);
        //             Debug.Assert(dev.GetMaxOutputReportLength() == reportDescriptor.MaxOutputReportLength);
        //             Debug.Assert(dev.GetMaxFeatureReportLength() == reportDescriptor.MaxFeatureReportLength);
        //
        //             foreach (var deviceItem in reportDescriptor.DeviceItems)
        //             {
        //                 Console.WriteLine(@"Opening device for 20 seconds...");
        //                 
        //                 if (dev.TryOpen(out var hidStream))
        //                 {
        //                     hidStream.ReadTimeout = Timeout.Infinite;
        //
        //                     using (hidStream)
        //                     {
        //                         var inputReportBuffer = new byte[dev.GetMaxInputReportLength()];
        //                         var inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
        //                         var inputParser = deviceItem.CreateDeviceItemInputParser();
        //
        //
        //                         inputReceiver.Received += (sender, e) =>
        //                         {
        //                             while (inputReceiver.TryRead(inputReportBuffer, 0, out var report))
        //                             {
        //                                 // Parse the report if possible.
        //                                 // This will return false if (for example) the report applies to a different DeviceItem.
        //                                 if (inputParser.TryParseReport(inputReportBuffer, 0, report))
        //                                 {
        //                                     // If you are using Windows Forms, you could call BeginInvoke here to marshal the results
        //                                     // to your main thread.
        //                                     WriteDeviceItemInputParserResult(inputParser);
        //                                 }
        //                             }
        //                         };
        //                         inputReceiver.Start(hidStream);
        //
        //                         Thread.Sleep(60000);
        //                     }
        //                 }
        //                 else
        //                 {
        //                     Console.WriteLine(@"Failed to open device.");
        //                 }
        //             }
        //         }
        //         catch (Exception e)
        //         {
        //             Console.WriteLine(e);
        //         }
        //     }
        // }
    }
}