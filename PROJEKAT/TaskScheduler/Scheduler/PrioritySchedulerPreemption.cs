using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;
using TaskScheduler.Scheduler;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerPreemption : PreemptiveScheduler
    {

        //protected Dictionary<JobContext, HashSet<JobContext>> inversionMap = new();
        

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

        internal override void HandleJobFinished(JobContext jobContext)
        {
            
            lock (schedulerLock)
            {
                if(jobContext.GetJobState() != JobContext.JobState.Stopped)
                {
                    jobContext.SetJobState(JobContext.JobState.Finished);
                }
                
                runningJobs.Remove(jobContext);
                jobContext.ReversePriority();
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                    
                }

                if(resourceMap.ContainsKey(jobContext))
                {
                    HashSet<ResourceClass> resources = resourceMap[jobContext];
                    foreach (var r in resources)
                    {
                        HandleResourceReleased(jobContext, r);
                    }
                }
                
            }
        }

        internal override void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                //Console.WriteLine("PRIORh: " + jobContext.Priority);
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
