using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        //TaskScheduler has MaxTasks, queue for jobs and current running jobs
        //Also lock is there
        public int MaxConcurrentTasks { get; set; } = 1;
        private readonly PriorityQueue<JobContext, int> jobQueue = new();
        private readonly HashSet<JobContext> runningJobs = new();
        private readonly object schedulerLock = new();

        public Job Schedule(JobSpecification jobSpecification)
        {
            //When scheduling, context for a job will be created
            //UserJob is implemented by DemoUserJob
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,                      
                priority: jobSpecification.Priority,                    //priority = jobs priority
                onJobFinished: HandleJobFinished,                       //all the handlers are implemented in task scheduler
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested);

            //Either start the job if it can be started
            //Or put in in waiting queue
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

        //Remove job from running jobs
        //And start a new one if it's there
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

        //Same logic as HandleJobFinished
        //But not implemented in the same place
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

        //Either run job instantly if there's space for it 
        //Or put it in queue
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
