using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

        public void OnUserChangedBrightness(IView.ViewBrightnessChangedEventArgs e)
        {
            Debug.Print("display recv ViewBrightnessChangedEventArgs");
        }

        public async void OnRefreshDisplayList()
        {
            await LoadMonitors();
        }
        
        private async Task LoadMonitors()
        {
            var monitorsTask = Task.Run(() => MonitorManager.EnumerateMonitors().ToList());
            var displayList = await monitorsTask;
            _model.SetDisplayList(displayList);
        }
    }
}