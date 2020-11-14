using tray_app_mvc.model;

namespace tray_app_mvc.controller
{
    public class MonitorUserController
    {
        private MonitorModel _model;

        public MonitorUserController(MonitorModel model)
        {
            _model = model;
        }

        public void SetBrightness(int brightness)
        {
            _model?.SetCurrentBrightness(brightness);
        }
    }
}