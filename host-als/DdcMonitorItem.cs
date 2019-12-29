using System;
using System.Runtime.InteropServices;
/// <summary>
/// Physical monitor controlled by DDC/CI (external monitor)
/// </summary>
internal class DdcMonitorItem : MonitorItem
{
    private readonly SafePhysicalMonitorHandle _handle;
    private readonly bool _useLowLevel;

    public DdcMonitorItem(
        string deviceInstanceId,
        string description,
        byte displayIndex,
        byte monitorIndex,
        SafePhysicalMonitorHandle handle,
        bool useLowLevel = false) : base(
            deviceInstanceId: deviceInstanceId,
            description: description,
            displayIndex: displayIndex,
            monitorIndex: monitorIndex,
            isReachable: true)
    {
        this._handle = handle ?? throw new ArgumentNullException(nameof(handle));
        this._useLowLevel = useLowLevel;
    }

    private uint _minimum = 0; // Raw minimum brightness (not always 0)
    private uint _maximum = 100; // Raw maximum brightness (not always 100)

    public override bool UpdateBrightness(int brightness = -1)
    {
        var (success, minimum, current, maximum) = MonitorConfiguration.GetBrightness(_handle, _useLowLevel);

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

    public override bool SetBrightness(int brightness)
    {
        if ((brightness < 0) || (100 < brightness))
            throw new ArgumentOutOfRangeException(nameof(brightness), brightness, "The brightness must be within 0 to 100.");

        var buffer = (uint)Math.Round(brightness / 100D * (_maximum - _minimum) + _minimum, MidpointRounding.AwayFromZero);

        if (MonitorConfiguration.SetBrightness(_handle, buffer, _useLowLevel))
        {
            this.Brightness = brightness;
            return true;
        }
        return false;
    }

    #region IDisposable

    private bool _isDisposed = false;

    protected override void Dispose(bool disposing)
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

        base.Dispose(disposing);
    }

    #endregion

    public override String ToString()
    {
        var format = "Brightness {0}, Description {1}, LowLevel {2}";
        return string.Format(format, this.Brightness, this.Description, this._useLowLevel);
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
        return MonitorConfiguration.DestroyPhysicalMonitor(handle);
    }
}