using System;

namespace tray_app_mcv.Model
{
    public class MonitorModel : IMonitorModel
    {
        public event ModelHandler<MonitorModel> changed;

        private int _brightness;

        public void setBrightness(int newBrightness) 
        { 
            _brightness = newBrightness; 
            changed.Invoke(this, new ModelEventArgs(_brightness));
        }

        public void attach(IModelObserver imo) 
        { 
            changed += new ModelHandler<MonitorModel>(imo.onModelEvent);
        }

    }
}