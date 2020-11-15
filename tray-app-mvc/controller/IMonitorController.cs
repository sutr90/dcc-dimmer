namespace tray_app_mvc.controller
{
    public interface IMonitorController
    {
        public void OnUserChangedBrightness(ViewBrightnessChangedEventArgs viewBrightnessChangedEventArgs);
    }
}