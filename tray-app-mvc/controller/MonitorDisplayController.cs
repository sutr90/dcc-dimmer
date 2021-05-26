using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tray_app_mvc.model;

namespace tray_app_mvc.controller
{
    public class MonitorDisplayController : IMonitorController
    {
        private readonly MonitorModel _model;

        private readonly CancellationTokenSource _displayWatchTokenSource = new CancellationTokenSource();

        public MonitorDisplayController(MonitorModel model)
        {
            _model = model;
            LaunchBrightnessWatchTask(_displayWatchTokenSource.Token);
        }

        public void OnUserChangedBrightness(ViewBrightnessChangedEventArgs e)
        {
            ChangeDisplayBrightness(e.Brightness);
        }

        public void ChangeDisplayBrightness(int newBrightness)
        {
            if (_model.Brightness == newBrightness) return;

            foreach (var m in _model.DisplayList)
            {
                m.SetDeviceBrightness(newBrightness);
            }
        }

        public async void OnRefreshDisplayList()
        {
            await LoadMonitors();
            _model.SetBrightness(ReadMonitorBrightness(0));
        }

        private void LaunchBrightnessWatchTask(CancellationToken cancellationToken)
        {
            var progress = new Progress<int>(s => { _model.SetBrightness(s); });

            Task.Factory.StartNew(() => WatchDisplayBrightness(cancellationToken, progress),
                TaskCreationOptions.LongRunning);
        }

        private void WatchDisplayBrightness(CancellationToken token, IProgress<int> progress)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var monitorBrightness = ReadMonitorBrightness(0);
                progress.Report(monitorBrightness);

                if (monitorBrightness == -1)
                {
                    OnRefreshDisplayList();
                }

                Task.Delay(5000, token).Wait(token);
            }
        }

        private int ReadMonitorBrightness(int monitorIndex)
        {
            var modelDisplayList = _model.DisplayList;
            if (modelDisplayList.Count <= 0) return -1;

            foreach (var monitor in modelDisplayList)
            {
                monitor.ReadDeviceBrightness();
            }

            return modelDisplayList[monitorIndex].Brightness;
        }

        private async Task LoadMonitors()
        {
            var monitorsTask = Task.Run(() => MonitorManager.EnumerateMonitors().ToList());
            var displayList = await monitorsTask;
            _model.SetDisplayList(displayList);
        }

        public void OnShutdown()
        {
            _displayWatchTokenSource.Cancel();
        }
    }
}