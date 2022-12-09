using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;

namespace TaskScheduler
{
    public class TaskScheduler
    {
        //TaskScheduler has MaxTasks, queue for jobs and current running jobs
        //Also lock is there
        public int MaxConcurrentTasks { get; set; } = 2;
        //private readonly PriorityQueue<JobContext, int> jobQueue = new();
        private readonly AbstractQueue jobQueue;
        private readonly HashSet<JobContext> runningJobs = new();
        private readonly object schedulerLock = new();

        public TaskScheduler(bool fifoflag) {
            if(fifoflag)
            {
                jobQueue = new FIFOQueue();
            } else
            {
                jobQueue = new PriorityQueue();
            }
        }

        public Job Schedule(JobSpecification jobSpecification)
        {
            //When scheduling, context for a job will be created
            //UserJob is implemented by DemoUserJob
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,                //priority = jobs priority
                startTime: jobSpecification.StartTime,
                finishTime: jobSpecification.FinishTime,
                maxExecutionTime: jobSpecification.MaxExecutionTime,                                 
                onJobFinished: HandleJobFinished,                       //all the handlers are implemented in task scheduler
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested,
                onJobStopped: HandleJobStopped);

            //Either start the job if it can be started
            //Or put in in waiting queue
            ScheduleJob(jobSpecification, jobContext);

            return new Job(jobContext);
        }

        public Job ScheduleWithStart(bool flag, JobSpecification jobSpecification)
        {
            if(flag)
            {
                return Schedule(jobSpecification);
            } else
            {
                return ScheduleWithoutStart(jobSpecification);
            }
            
        }

        private Job ScheduleWithoutStart(JobSpecification jobSpecification)
        {
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,                    //priority = jobs priority
                startTime: jobSpecification.StartTime,
                finishTime: jobSpecification.FinishTime,
                maxExecutionTime: jobSpecification.MaxExecutionTime,
                onJobFinished: HandleJobFinished,                       //all the handlers are implemented in task scheduler
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested,
                onJobStopped: HandleJobStopped);

            lock (schedulerLock)
            {
                jobQueue.Enqueue(jobContext, jobSpecification.Priority);
            }

            return new Job(jobContext);
        }

        private void ScheduleJob(JobSpecification jobSpecification, JobContext jobContext)
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
                    jobQueue.Enqueue(jobContext, jobSpecification.Priority);
                    
                }
            }
        }


        public Job StartJobOnSeparateProcess(JobSpecification jobSpecification)
        {
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,                    //priority = jobs priority
                startTime: jobSpecification.StartTime,
                finishTime: jobSpecification.FinishTime,
                maxExecutionTime: jobSpecification.MaxExecutionTime,
                onJobFinished: HandleJobFinished,                       //all the handlers are implemented in task scheduler
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested,
                onJobStopped: HandleJobStopped);

            return new Job(jobContext);
        }

        //Remove job from running jobs
        //And start a new one if it's there
        private void HandleJobFinished(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if(jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        //Same logic as HandleJobFinished
        //But not implemented in the same place
        //Don't want to duplicate code
        private void HandleJobPaused(JobContext jobContext)
        {
            lock(schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if(jobQueue.Count() > 0)
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
            if(jobContext.GetJobState() == JobContext.JobState.Stopped)
            {
                throw new InvalidOperationException("Can't started a stopped job!");
            }

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

        private void HandleJobStopped(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

    }
}
