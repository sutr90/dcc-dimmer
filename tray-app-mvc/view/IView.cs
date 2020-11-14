using System;

namespace tray_app_mvc.view
{
    public interface IView
    {
        public class ViewBrightnessChangedEventArgs : EventArgs
        {
            public int Brightness { get; set; }
        }

        public event EventHandler<ViewBrightnessChangedEventArgs> BrightnessChanged;
    }
}