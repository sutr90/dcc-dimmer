using System;
using System.Collections.Generic;
using System.Linq;

namespace tray_app_mvc
{
    internal static class MonitorManager
    {
        public static IEnumerable<DdcMonitorItem> EnumerateMonitors()
        {
            var deviceItems = DeviceContext.EnumerateMonitorDevices().ToList();

            if (deviceItems.Any() != true)
                yield break;

            // Obtained by DDC/CI
            foreach (var handleItem in DeviceContext.GetMonitorHandles())
            {
                foreach (var physicalItem in MonitorApi.EnumeratePhysicalMonitors(handleItem.MonitorHandle))
                {
                    var index = -1;
                    if (physicalItem.IsSupported)
                    {
                        index = deviceItems.FindIndex(x =>
                            x.DisplayIndex == handleItem.DisplayIndex &&
                            x.MonitorIndex == physicalItem.MonitorIndex &&
                            string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
                    }

                    if (index < 0)
                    {
                        physicalItem.Handle.Dispose();
                        continue;
                    }

                    var deviceItem = deviceItems[index];
                    yield return new DdcMonitorItem(
                        deviceItem.DeviceInstanceId,
                        deviceItem.Description,
                        deviceItem.DisplayIndex,
                        deviceItem.MonitorIndex,
                        physicalItem.Handle);

                    deviceItems.RemoveAt(index);
                    if (deviceItems.Count == 0)
                        yield break;
                }
            }
        }
    }
}