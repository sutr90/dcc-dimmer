using System;
using tray_app_mcv.View;
using tray_app_mcv.Model;

namespace tray_app_mcv.Controller
{
    // The Icontroller supports only one functionality that is to increment the value
    public interface IController
    {
    }

    public interface IMonitorController : IController {
        void setBrightness(int brightness);
    }

    public class MonitorController : IMonitorController
    {
        IView view;
        MonitorModel model;

        // The controller which implements the IController interface ties the view and model 
        // together and attaches the view via the IModelInterface and addes the event
        // handler to the view_changed function. The view ties the controller to itself.
        public MonitorController(IView v, MonitorModel m)
        {
            view = v;
            model = m;
            view.setController(this);
            model.attach((IModelObserver)view);
            view.changed += new ViewHandler<IView>(this.view_changed);
        }

        // event which gets fired from the view when the users changes the value
        public void view_changed(IView v, ViewEventArgs e)
        {
            model.setBrightness(e.value);
        }

        // This does the actual work of getting the model do the work
        public void setBrightness(int b)
        {
            model.setBrightness(b);
        }

    }
}
