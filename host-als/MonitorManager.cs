using System;
using System.Collections.Generic;
using System.Linq;
class MonitorManager
{
    #region Type

    private class DeviceItemPlus
    {
        private readonly DeviceContext.DeviceItem _deviceItem;

        public string DeviceInstanceId => _deviceItem.DeviceInstanceId;
        public string Description => _deviceItem.Description;
        public string AlternateDescription { get; }
        public byte DisplayIndex => _deviceItem.DisplayIndex;
        public byte MonitorIndex => _deviceItem.MonitorIndex;

        public DeviceItemPlus(
            DeviceContext.DeviceItem deviceItem,
            string alternateDescription = null)
        {
            this._deviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem));
            this.AlternateDescription = alternateDescription ?? deviceItem.Description;
        }
    }

    #endregion

    public static IEnumerable<DdcMonitorItem> EnumerateMonitors()
    {
        var deviceItems = GetMonitorDevices();

        return EnumerateMonitors(deviceItems);
    }

    private static List<DeviceItemPlus> GetMonitorDevices()
    {
        return DeviceContext.EnumerateMonitorDevices().Select(x => new DeviceItemPlus(x)).ToList();
    }

    private static IEnumerable<DdcMonitorItem> EnumerateMonitors(List<DeviceItemPlus> deviceItems)
    {
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
                    description: deviceItem.AlternateDescription,
                    displayIndex: deviceItem.DisplayIndex,
                    monitorIndex: deviceItem.MonitorIndex,
                    handle: physicalItem.Handle,
                    useLowLevel: physicalItem.IsLowLevelSupported);

                deviceItems.RemoveAt(index);
                if (deviceItems.Count == 0)
                    yield break;
            }
        }
    }

}