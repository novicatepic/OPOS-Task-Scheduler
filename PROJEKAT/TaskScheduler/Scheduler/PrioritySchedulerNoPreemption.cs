using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerNoPreemption : NonPreemptiveScheduler
    {
        public PrioritySchedulerNoPreemption()
        {
            jobQueue = new Queue.PriorityQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                if (runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                else
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);

                }
            }            
        }
    }
}
