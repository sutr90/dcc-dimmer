﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            var displayWatchTokenSource = new CancellationTokenSource();
            LaunchBrightnessWatchTask(displayWatchTokenSource.Token);
        }

        public void OnUserChangedBrightness(IView.ViewBrightnessChangedEventArgs e)
        {
            Debug.Print("display recv ViewBrightnessChangedEventArgs");
            
            foreach (var m in _model.DisplayList)
            {
                m.SetDeviceBrightness(e.Brightness);
            }
        }

        public async void OnRefreshDisplayList()
        {
            await LoadMonitors();
        }

        private void LaunchBrightnessWatchTask(CancellationToken cancellationToken)
        {
            
            var progress = new Progress<string>(s =>
            {
                Debug.Print("Watch Task Progress {0}", s);
                
                if (int.TryParse(s, out var newBrightness))
                {
                    _model.SetCurrentBrightness(newBrightness);
                }
            });
            
            Debug.Print("Watch Task Started");
            
            Task.Factory.StartNew(() => LongWork(cancellationToken, progress), TaskCreationOptions.LongRunning);
        }

        private void LongWork(CancellationToken token, IProgress<string> progress)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.Print("Cancellation requested");
                    break;
                }

                var modelDisplayList = _model.DisplayList;
                if (modelDisplayList.Count > 0)
                {
                    foreach (var monitor in modelDisplayList)
                    {
                        monitor.ReadDeviceBrightness();
                    }

                    // TODO: tady to chce zmenit logiku, aby to bralo v potaz vybrany monitor a ne vzdy prvni
                    progress.Report(modelDisplayList[0].Brightness.ToString());
                }
                
                Task.Delay(5000, token).Wait(token);
            }
        }

        private async Task LoadMonitors()
        {
            var monitorsTask = Task.Run(() => MonitorManager.EnumerateMonitors().ToList());
            var displayList = await monitorsTask;
            _model.SetDisplayList(displayList);
        }
    }
}