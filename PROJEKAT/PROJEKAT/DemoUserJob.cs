using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler;

namespace PROJEKAT
{
    public class DemoUserJob : IUserJob
    {
        public string Name { get; init; } = "DemoUserJob";
        public int NumIterations = 100;
        public int SleepTime = 500;

        public void Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");

            for(int i = 0; i < NumIterations; i++) {
                Console.WriteLine($"{Name}: {i}");
                Thread.Sleep(SleepTime);
                jobApi.CheckForPause();
            }

            Console.WriteLine($"{Name} finished.");
        }
    }
}
