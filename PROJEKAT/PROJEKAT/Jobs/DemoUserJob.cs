using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler;

namespace PROJEKAT.Jobs
{
    public class DemoUserJob : IUserJob
    {
        //Job has it's name, number of iterations, sleep time...
        //And it implements Run method where we can implement our logic
        public string Name { get; init; } = "DemoUserJob";
        public int parallelism { get; init; } = 2;

        public int NumIterations = 100;
        public int SleepTime = 500;

        //Important note: checking for pause at the end of each iteration
        public void Run(IJobContext jobApi)
        {
            Console.WriteLine(Name + " started.");

            //DOESN'T MAKE SENSE TO DO THIS HERE
            /*Parallel.For(0, NumIterations, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, i =>
            {
                Console.WriteLine($"{Name}: {i}");
                Thread.Sleep(SleepTime);
                //Console.WriteLine($"{Name}: {i}");
            });*/

            for (int i = 0; i < NumIterations; i++)
            {
                Console.WriteLine($"{Name}: {i}");
                Thread.Sleep(SleepTime);
                if (jobApi.StoppageConfirmed())
                {
                    break;
                }

                jobApi.CheckAll();

                if (jobApi.CheckConditions())
                {
                    break;
                }
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
