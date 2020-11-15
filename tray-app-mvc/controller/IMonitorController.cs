using tray_app_mvc.view;

namespace tray_app_mvc.controller
{
    public interface IMonitorController
    {
        public void OnUserChangedBrightness(object sender, IView.ViewBrightnessChangedEventArgs e);
    }
}