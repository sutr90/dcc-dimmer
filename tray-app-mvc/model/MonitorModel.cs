using System;
using System.Diagnostics;

namespace tray_app_mvc.model
{
    public class MonitorModel
    {
        private int _currentBrightness = -1;

        public void SetCurrentBrightness(int currentBrightness)
        {
            _currentBrightness = currentBrightness;
            var args = new ModelBrightnessChangedEventArgs {Brightness = _currentBrightness};
            DispatchBrightnessChanged(args);
        }
        
        private void DispatchBrightnessChanged(ModelBrightnessChangedEventArgs e)
        {
            var handler = BrightnessChanged;
            Debug.Print("model raise ModelBrightnessChangedEventArgs");
            handler?.Invoke(this, e);
        }
        
        public event EventHandler<ModelBrightnessChangedEventArgs> BrightnessChanged;
    }

    public class ModelBrightnessChangedEventArgs : EventArgs
    {
        public int Brightness { get; set; }
    }
}