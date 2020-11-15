using System.Diagnostics;
using tray_app_mvc.model;
using tray_app_mvc.view;

namespace tray_app_mvc.controller
{
    public class MonitorDisplayController : IMonitorController
    {
        private readonly MonitorModel _model;

        public MonitorDisplayController(MonitorModel model)
        {
            _model = model;
        }

        public void OnUserChangedBrightness(object sender, IView.ViewBrightnessChangedEventArgs e)
        {
            Debug.Print("display recv ViewBrightnessChangedEventArgs");
        }
    }
}