using System;
using System.Collections.Generic;
using System.Linq;

namespace host_als
{
    class Program
    {
        static void Main(string[] args)
        {
            List<DdcMonitorItem> result = MonitorManager.EnumerateMonitors().ToList();

            foreach(var m in result){
                m.UpdateBrightness();
                Console.WriteLine(m);
                //m.SetBrightness(40);
            }
        }            
    }
}
