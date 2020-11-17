namespace tray_app_mvc.controller
{
    public interface IMonitorController : IController
    {
        public void OnUserChangedBrightness(ViewBrightnessChangedEventArgs viewBrightnessChangedEventArgs);
    }
}