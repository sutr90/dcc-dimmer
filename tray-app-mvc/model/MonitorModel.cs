using System;

namespace tray_app_mvc.model
{
    public class MonitorModel
    {
        private int _currentBrightness = -1;

        public void SetCurrentBrightness(int currentBrightness)
        {
            _currentBrightness = currentBrightness;
            var args = new BrightnessChangedEventArgs {Brightness = _currentBrightness};
            OnBrightnessChanged(args);
        }
        
        private void OnBrightnessChanged(BrightnessChangedEventArgs e)
        {
            var handler = BrightnessChanged;
            handler?.Invoke(this, e);
        }
        
        public event EventHandler<BrightnessChangedEventArgs> BrightnessChanged;
    }

    public class BrightnessChangedEventArgs : EventArgs
    {
        public int Brightness { get; set; }
    }
}