using System;
using System.Runtime.InteropServices;
/// <summary>
/// Physical monitor controlled by DDC/CI (external monitor)
/// </summary>
internal class DdcMonitorItem
{

    
    public string DeviceInstanceId { get; }
    public string Description { get; }
    public byte DisplayIndex { get; }
    public byte MonitorIndex { get; }
    public bool IsReachable { get; }

    public int Brightness { get; protected set; } = -1;
    public int BrightnessSystemAdjusted { get; protected set; } = -1;
    private readonly SafePhysicalMonitorHandle _handle;

    public DdcMonitorItem(
        string deviceInstanceId,
        string description,
        byte displayIndex,
        byte monitorIndex,
        SafePhysicalMonitorHandle handle)
    {
        this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
            throw new ArgumentNullException(nameof(deviceInstanceId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentNullException(nameof(description));

        this.DeviceInstanceId = deviceInstanceId;
        this.Description = description;
        this.DisplayIndex = displayIndex;
        this.MonitorIndex = monitorIndex;
    }

    private uint _minimum = 0; // Raw minimum brightness (not always 0)
    private uint _maximum = 100; // Raw maximum brightness (not always 100)

    public bool UpdateBrightness(int brightness = -1)
    {
        var (success, minimum, current, maximum) = MonitorApi.GetBrightness(_handle);

        if (!success || !(minimum < maximum) || !(minimum <= current) || !(current <= maximum))
        {
            this.Brightness = -1;
            return false;
        }
        this.Brightness = (int)Math.Round((double)(current - minimum) / (maximum - minimum) * 100D, MidpointRounding.AwayFromZero);
        this._minimum = minimum;
        this._maximum = maximum;
        return true;
    }

    public bool SetBrightness(int brightness)
    {
        if ((brightness < 0) || (100 < brightness))
            throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

        var buffer = (uint)Math.Round(brightness / 100D * (_maximum - _minimum) + _minimum, MidpointRounding.AwayFromZero);

        if (MonitorApi.SetBrightness(_handle, buffer))
        {
            this.Brightness = brightness;
            return true;
        }
        return false;
    }

    #region IDisposable

    private bool _isDisposed = false;

    protected void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            _handle.Dispose();
        }

        // Free any unmanaged objects here.
        _isDisposed = true;
    }

    #endregion

    public override String ToString()
    {
        var format = "Brightness {0}, Description {1}, DisplayIndex {2}, MonitorIndex {3}";
        return string.Format(format, this.Brightness, this.Description, this.DisplayIndex, this.MonitorIndex);
    }
}

internal class SafePhysicalMonitorHandle : SafeHandle
{
    public SafePhysicalMonitorHandle(IntPtr handle) : base(IntPtr.Zero, true)
    {
        this.handle = handle; // IntPtr.Zero may be a valid handle.
    }

    public override bool IsInvalid => false; // The validity cannot be checked by the handle.

    protected override bool ReleaseHandle()
    {
        return MonitorApi.DestroyPhysicalMonitor(handle);
    }
}