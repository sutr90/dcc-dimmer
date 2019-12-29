using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace host_als
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            IEnumerable<DdcMonitorItem> result = MonitorManager.EnumerateMonitors();

            foreach(var m in result){
                m.UpdateBrightness();
                Console.WriteLine(m);

                m.SetBrightness(40);
            }
        }            
    }
}
