using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Scheduler;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerPreemption : AbstractScheduler, IPreemption
    {
        public PrioritySchedulerPreemption()
        {
            jobQueue = new Queue.PriorityQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                if (runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                else
                {
                    CheckPreemption(jobContext);
                    
                }
            }
        }

        public void CheckPreemption(JobContext jobContext)
        {
            jobQueue.Enqueue(jobContext, jobContext.Priority);
            int maxPriority = -1;
            int index = -1;
            for (int i = 0; i < runningJobs.Count; i++)
            {
                if (runningJobs.ElementAt(i).Priority > jobContext.Priority && runningJobs.ElementAt(i).Priority > maxPriority)
                {
                    maxPriority = runningJobs.ElementAt(i).Priority;
                    index = i;
                }
            }
            if (index != -1)
            {
                runningJobs.ElementAt(index).RequestPriorityStoppage();
            }
        }

        internal override void HandleJobFinished(JobContext jobContext)
        {
            
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    Console.WriteLine("LOL1!");
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    if (dequeuedJobContext.shouldWaitForPriority)
                    {
                        //Console.WriteLine("LOL!");
                        //dequeuedJobContext.shouldWaitForPriority = false;
                       // dequeuedJobContext.prioritySemaphore.Release();
                        dequeuedJobContext.Start();
                        
                    } 
                    else
                    {
                        runningJobs.Add(dequeuedJobContext);
                        dequeuedJobContext.Start();
                    }
                    
                }
            }
        }

        internal override void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                Console.WriteLine("PRIORh: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
                if(jobContext.shouldWaitForPriority)
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
            }
        }

    }
}
