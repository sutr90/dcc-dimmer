using System;

using tray_app_mcv.Model;
using tray_app_mcv.Controller;

namespace tray_app_mcv.View
{
        public delegate void ViewHandler<IView>(IView sender, ViewEventArgs e);

        // The event arguments class that will be used while firing the events
        // for this program, we have only one value which the user changed.
        public class ViewEventArgs: EventArgs 
        {
            public int value;
            public ViewEventArgs(int v) { value = v; }
        }


        // Currently, the interface only contains the method to set the controller to which
        // it is tied. The rest of the view related code is implemented in the form.
        public interface IView
        {
            event ViewHandler<IView> changed;
            void setController(IController cont);
        }

}