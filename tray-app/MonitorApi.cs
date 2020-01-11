using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;


class MonitorApi
{
    #region Win32

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyPhysicalMonitor(
        IntPtr hMonitor);

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorCapabilities(
        SafePhysicalMonitorHandle hMonitor,
        out MC_CAPS pdwMonitorCapabilities,
        out MC_SUPPORTED_COLOR_TEMPERATURE pdwSupportedColorTemperatures);

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorBrightness(
        SafePhysicalMonitorHandle hMonitor,
        out uint pdwMinimumBrightness,
        out uint pdwCurrentBrightness,
        out uint pdwMaximumBrightness);

    [DllImport("Dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMonitorBrightness(
        SafePhysicalMonitorHandle hMonitor,
        uint dwNewBrightness);

    
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [Flags]
    private enum MC_CAPS
    {
        MC_CAPS_NONE = 0x00000000,
        MC_CAPS_MONITOR_TECHNOLOGY_TYPE = 0x00000001,
        MC_CAPS_BRIGHTNESS = 0x00000002,
        MC_CAPS_CONTRAST = 0x00000004,
        MC_CAPS_COLOR_TEMPERATURE = 0x00000008,
        MC_CAPS_RED_GREEN_BLUE_GAIN = 0x00000010,
        MC_CAPS_RED_GREEN_BLUE_DRIVE = 0x00000020,
        MC_CAPS_DEGAUSS = 0x00000040,
        MC_CAPS_DISPLAY_AREA_POSITION = 0x00000080,
        MC_CAPS_DISPLAY_AREA_SIZE = 0x00000100,
        MC_CAPS_RESTORE_FACTORY_DEFAULTS = 0x00000400,
        MC_CAPS_RESTORE_FACTORY_COLOR_DEFAULTS = 0x00000800,
        MC_RESTORE_FACTORY_DEFAULTS_ENABLES_MONITOR_SETTINGS = 0x00001000
    }

    [Flags]
    private enum MC_SUPPORTED_COLOR_TEMPERATURE
    {
        MC_SUPPORTED_COLOR_TEMPERATURE_NONE = 0x00000000,
        MC_SUPPORTED_COLOR_TEMPERATURE_4000K = 0x00000001,
        MC_SUPPORTED_COLOR_TEMPERATURE_5000K = 0x00000002,
        MC_SUPPORTED_COLOR_TEMPERATURE_6500K = 0x00000004,
        MC_SUPPORTED_COLOR_TEMPERATURE_7500K = 0x00000008,
        MC_SUPPORTED_COLOR_TEMPERATURE_8200K = 0x00000010,
        MC_SUPPORTED_COLOR_TEMPERATURE_9300K = 0x00000020,
        MC_SUPPORTED_COLOR_TEMPERATURE_10000K = 0x00000040,
        MC_SUPPORTED_COLOR_TEMPERATURE_11500K = 0x00000080
    }

    #endregion

    #region Type

    [DataContract]
    public class PhysicalItem
    {
        [DataMember(Order = 0)]
        public string Description { get; private set; }

        [DataMember(Order = 1)]
        public int MonitorIndex { get; private set; }

        public SafePhysicalMonitorHandle Handle { get; }

        public bool IsSupported => IsHighLevelSupported;

        [DataMember(Order = 2)]
        public bool IsHighLevelSupported { get; private set; }
        public PhysicalItem(
            string description,
            int monitorIndex,
            SafePhysicalMonitorHandle handle,
            bool isHighLevelSupported)
        {
            this.Description = description;
            this.MonitorIndex = monitorIndex;
            this.Handle = handle;
            this.IsHighLevelSupported = isHighLevelSupported;
        }
    }

    #endregion

    public static IEnumerable<PhysicalItem> EnumeratePhysicalMonitors(IntPtr monitorHandle)
    {
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(
            monitorHandle,
            out uint count))
        {
            Debug.WriteLine($"Failed to get the number of physical monitors. ");
            yield break;
        }
        if (count == 0)
        {
            yield break;
        }

        var physicalMonitors = new PHYSICAL_MONITOR[count];

        try
        {
            if (!GetPhysicalMonitorsFromHMONITOR(
                monitorHandle,
                count,
                physicalMonitors))
            {
                Debug.WriteLine($"Failed to get an array of physical monitors. ");
                yield break;
            }

            int monitorIndex = 0;

            foreach (var physicalMonitor in physicalMonitors)
            {
                var handle = new SafePhysicalMonitorHandle(physicalMonitor.hPhysicalMonitor);

                bool isHighLevelSupported = GetMonitorCapabilities(
                    handle,
                    out MC_CAPS caps,
                    out _)
                    && caps.HasFlag(MC_CAPS.MC_CAPS_BRIGHTNESS);


                yield return new PhysicalItem(
                    description: physicalMonitor.szPhysicalMonitorDescription,
                    monitorIndex: monitorIndex,
                    handle: handle,
                    isHighLevelSupported: isHighLevelSupported);

                monitorIndex++;
            }
        }
        finally
        {
            // The physical monitor handles should be destroyed at a later stage.
        }
    }

    /// <summary>
    /// Gets raw brightnesses not represented in percentage.
    /// </summary>
    /// <param name="physicalMonitorHandle">Physical monitor handle</param>
    /// <returns>
    /// <para>success: True if successfully gets</para>
    /// <para>minimum: Raw minimum brightness (not always 0)</para>
    /// <para>current: Raw current brightness (not always 0 to 100)</para>
    /// <para>maximum: Raw maximum brightness (not always 100)</para>
    /// </returns>
    /// <remarks>
    /// Raw minimum and maximum brightnesses will become meaningful when they are not standard
    /// values (0 and 100) and so raw current brightness needs to be converted to brightness
    /// in percentage using those values. They are used to convert brightness in percentage
    /// back to raw brightness when settings brightness as well.
    /// </remarks>
    public static (bool success, uint minimum, uint current, uint maximum) GetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle)
    {
        if (physicalMonitorHandle is null)
            throw new ArgumentNullException(nameof(physicalMonitorHandle));

        if (physicalMonitorHandle.IsClosed)
        {
            Debug.WriteLine("Failed to get brightnesses. The physical monitor handle has been closed.");
            return (success: false, 0, 0, 0);
        }


        if (!GetMonitorBrightness(
            physicalMonitorHandle,
            out uint minimumBrightness,
            out uint currentBrightness,
            out uint maximumBrightness))
        {
            Debug.WriteLine($"Failed to get brightnesses. ");
            return (success: false, 0, 0, 0);
        }
        return (success: true,
            minimum: minimumBrightness,
            current: currentBrightness,
            maximum: maximumBrightness);
    }

    /// <summary>
    /// Sets raw brightness not represented in percentage.
    /// </summary>
    /// <param name="physicalMonitorHandle">Physical monitor handle</param>
    /// <param name="brightness">Raw brightness (not always 0 to 100)</param>
    /// <param name="useLowLevel">Whether to use low level function</param>
    /// <returns>True if successfully sets</returns>
    public static bool SetBrightness(SafePhysicalMonitorHandle physicalMonitorHandle, uint brightness)
    {
        if (physicalMonitorHandle is null)
            throw new ArgumentNullException(nameof(physicalMonitorHandle));

        if (physicalMonitorHandle.IsClosed)
        {
            Debug.WriteLine("Failed to set brightness. The physical monitor handle has been closed.");
            return false;
        }

        if (!SetMonitorBrightness(
            physicalMonitorHandle,
            brightness))
        {
            Debug.WriteLine($"Failed to set brightness. ");
            return false;
        }
        return true;
    }
}