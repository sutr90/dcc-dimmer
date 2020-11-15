using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace tray_app_mvc.model
{
    public class MonitorModel
    {
        private int _currentBrightness = -1;

        private List<DdcMonitorItem> _displayList;

        public MonitorModel()
        {
            _displayList = new List<DdcMonitorItem>();
        }

        public void SetCurrentBrightness(int currentBrightness)
        {
            _currentBrightness = currentBrightness;
            var args = new ModelBrightnessChangedEventArgs {Brightness = _currentBrightness};
            DispatchBrightnessChanged(args);
        }

        public void SetDisplayList(List<DdcMonitorItem> displayList)
        {
            _displayList = displayList;
            var args = new DisplayListChangedEventArgs {DisplayList = _displayList};
            DispatchDisplayListChanged(args);
        }

        private void DispatchBrightnessChanged(ModelBrightnessChangedEventArgs e)
        {
            Debug.Print("model raise ModelBrightnessChangedEventArgs");
            BrightnessChanged?.Invoke(e);
        }

        private void DispatchDisplayListChanged(DisplayListChangedEventArgs e)
        {
            Debug.Print("model raise DisplayListChangedEventArgs");
            DisplayListChanged?.Invoke(e);
        }

        public event Action<ModelBrightnessChangedEventArgs> BrightnessChanged;
        public event Action<DisplayListChangedEventArgs> DisplayListChanged;
    }

    public class ModelBrightnessChangedEventArgs : EventArgs
    {
        public int Brightness { get; set; }
    }

    public class DisplayListChangedEventArgs : EventArgs
    {
        public List<DdcMonitorItem> DisplayList { get; set; }
    }
}