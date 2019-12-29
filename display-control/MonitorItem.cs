using System;

internal abstract class MonitorItem : IMonitor, IDisposable
{
    public string DeviceInstanceId { get; }
    public string Description { get; }
    public byte DisplayIndex { get; }
    public byte MonitorIndex { get; }
    public bool IsReachable { get; }

    public int Brightness { get; protected set; } = -1;
    public int BrightnessSystemAdjusted { get; protected set; } = -1;

    public MonitorItem(
        string deviceInstanceId,
        string description,
        byte displayIndex,
        byte monitorIndex,
        bool isReachable)
    {
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
            throw new ArgumentNullException(nameof(deviceInstanceId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentNullException(nameof(description));

        this.DeviceInstanceId = deviceInstanceId;
        this.Description = description;
        this.DisplayIndex = displayIndex;
        this.MonitorIndex = monitorIndex;
        this.IsReachable = isReachable;
    }

    public abstract bool UpdateBrightness(int brightness = -1);
    public abstract bool SetBrightness(int brightness);

    #region IDisposable

    private bool _isDisposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
        }

        // Free any unmanaged objects here.
        _isDisposed = true;
    }

    #endregion
}