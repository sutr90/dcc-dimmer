using tray_app_mvc.model;

namespace tray_app_mvc.controller
{
    public class MonitorUserController : IMonitorController
    {
        private readonly MonitorModel _model;

        public MonitorUserController(MonitorModel model)
        {
            _model = model;
        }

        private void SetBrightness(int brightness)
        {
            _model.SetBrightness(brightness);
        }

        public void OnUserChangedBrightness(ViewBrightnessChangedEventArgs e)
        {
            SetBrightness(e.Brightness);
        }

        public void OnShutdown()
        {
        }
    }
}