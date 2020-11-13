using System;

namespace tray_app_mcv.Model
{
    public delegate void ModelHandler<IModel>(IModel sender, ModelEventArgs e);

    public class ModelEventArgs : EventArgs
    {
        public int newBrightness;
        public ModelEventArgs(int b) 
        { 
            newBrightness = b; 
        }
    }

    // The interface which the form/view must implement so that, the event will be
    // fired when a value is changed in the model.
    public interface IModelObserver
    {
        void onModelEvent(IModel model, ModelEventArgs e);
    }

    // The Model interface where we can attach the function to be notified when value
    // is changed. An actual data manipulation function increment which increments the value
    // A setvalue function which sets the value when users changes the textbox
    public interface IModel
    {
        void attach(IModelObserver imo);
    }

    public interface IMonitorModel : IModel {
        void setBrightness(int newBrightness);
    }
}