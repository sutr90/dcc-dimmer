using System;
using System.Collections.Generic;
using System.Linq;
class MonitorManager
{
    public static IEnumerable<DdcMonitorItem> EnumerateMonitors()
    {
        var deviceItems = DeviceContext.EnumerateMonitorDevices().ToList();

        if (!(deviceItems?.Any() == true))
            yield break;

        // Obtained by DDC/CI
        foreach (var handleItem in DeviceContext.GetMonitorHandles())
        {
            foreach (var physicalItem in MonitorConfiguration.EnumeratePhysicalMonitors(handleItem.MonitorHandle))
            {
                int index = -1;
                if (physicalItem.IsSupported)
                {
                    index = deviceItems.FindIndex(x =>
                        (x.DisplayIndex == handleItem.DisplayIndex) &&
                        (x.MonitorIndex == physicalItem.MonitorIndex) &&
                        string.Equals(x.Description, physicalItem.Description, StringComparison.OrdinalIgnoreCase));
                }
                if (index < 0)
                {
                    physicalItem.Handle.Dispose();
                    continue;
                }

                var deviceItem = deviceItems[index];
                yield return new DdcMonitorItem(
                    deviceInstanceId: deviceItem.DeviceInstanceId,
                    description: deviceItem.Description,
                    displayIndex: deviceItem.DisplayIndex,
                    monitorIndex: deviceItem.MonitorIndex,
                    handle: physicalItem.Handle);

                deviceItems.RemoveAt(index);
                if (deviceItems.Count == 0)
                    yield break;
            }
        }
    }

}