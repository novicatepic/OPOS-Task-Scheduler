using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        public int MaxConcurrentTasks { get; set; } = 1;
        private readonly PriorityQueue<JobContext, int> jobQueue = new();
        private readonly HashSet<JobContext> runningJobs = new();
        private readonly object schedulerLock = new();

        public Job Schedule(JobSpecification jobSpecification)
        {
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,
                onJobFinished: HandleJobFinished, 
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested);

            lock(schedulerLock)
            {
                if(runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                else
                {
                    jobQueue.Enqueue(jobContext, jobSpecification.Priority);
                }
            }

            return new Job(jobContext);
        }

        private void HandleJobFinished(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if(jobQueue.Count > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        private void HandleJobPaused(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if(jobQueue.Count > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        private void HandleJobContinueRequested(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                if(runningJobs.Count < MaxConcurrentTasks)
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
