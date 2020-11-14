using System.Diagnostics;
using tray_app_mvc.model;
using tray_app_mvc.view;

namespace tray_app_mvc.controller
{
    public class MonitorUserController
    {
        private readonly MonitorModel _model;

        public MonitorUserController(MonitorModel model)
        {
            _model = model;
        }

        public void SetBrightness(int brightness)
        {
            _model?.SetCurrentBrightness(brightness);
        }

        public void OnUserChangedBrightness(object? sender, IView.ViewBrightnessChangedEventArgs e)
        {
            Debug.Print("ctl recv ViewBrightnessChangedEventArgs");
            SetBrightness(e.Brightness);
        }
    }
}