using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler;

namespace PROJEKAT.Jobs
{

    internal class DemoPIP : IUserJob
    {

        static ResourceClass r = new ResourceClass("R1");

        //Job has it's name, number of iterations, sleep time...
        //And it implements Run method where we can implement our logic
        public string Name { get; init; } = "PIPJob";

        public int NumIterations { get; init; } = 10;
        public int SleepTime { get; init; } = 500;

        public DemoPIP() { }
        bool resourceTaken = false;
        public void Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");

            for (int i = 1; i <= NumIterations; i++)
            {
                Console.WriteLine($"{Name}: {i}");

                double progress = (double)i / (double)NumIterations;
                jobApi.SetProgress(progress);

                if(resourceTaken == false)
                {
                    resourceTaken = true;
                    Console.WriteLine("IN");
                    jobApi.RequestResource(r);
                }
                
                jobApi.CheckAll();

                if (jobApi.StoppageConfirmed())
                {
                    break;
                }

                if (jobApi.CheckConditions())
                {
                    //Console.WriteLine("BROKE");
                    break;
                }
                Thread.Sleep(SleepTime);
            }

            if (!jobApi.StoppageConfirmed())
            {
                Console.WriteLine($"{Name} finished.");
            }
            else
            {
                Console.WriteLine($"{Name} stopped.");
            }

        }

    }
}
