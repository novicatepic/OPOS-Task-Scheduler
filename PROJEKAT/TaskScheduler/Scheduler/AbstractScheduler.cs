using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public abstract class AbstractScheduler
    {
        public int MaxConcurrentTasks { get; set; } = 1;
        protected AbstractQueue jobQueue;
        internal readonly HashSet<JobContext> runningJobs = new();
        private readonly HashSet<Job> jobsWihoutStart = new();
        protected readonly object schedulerLock = new();
        public static bool isOne = false;

        private Job Schedule(JobSpecification jobSpecification)
        {
            //Want to disable wait() callouts when there is one job running => better implementation
            if (MaxConcurrentTasks == 1)
            {
                isOne = true;
            }
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
                onJobStopped: HandleJobStopped,
                onJobStarted: HandleJobStartTime,
                onJobWait: HandleJobWaiting,
                isSeparate: false);

            //Either start the job if it can be started
            //Or put in in waiting queue
            if (jobSpecification.StartTime < DateTime.Now)
            {
                ScheduleJob(jobContext);
            }
            else
            {
                jobContext.CheckStartTime();
            }


            return new Job(jobContext);
        }

        public Job AddJobWithScheduling(JobSpecification jobSpecification)
        {
            return Schedule(jobSpecification);
        }

        public Job AddJobWithoutScheduling(JobSpecification jobSpecification)
        {
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,                //priority = jobs priority
                startTime: jobSpecification.StartTime,
                finishTime: jobSpecification.FinishTime,
                maxExecutionTime: jobSpecification.MaxExecutionTime,
                onJobFinished: HandleJobFinished,                       //all the handlers are implemented in task scheduler
                onJobPaused: HandleJobPaused,
                onJobContinueRequested: HandleJobContinueRequested,
                onJobStopped: HandleJobStopped,
                onJobStarted: HandleJobStartTime,
                onJobWait: HandleJobWaiting,
                isSeparate: false);

            jobContext.SetJobState(JobContext.JobState.NotScheduled);

            Job job = new Job(jobContext);
            jobsWihoutStart.Add(job);

            return job;
        }

        public void ScheduleUnscheduledJob(Job job)
        {
            if (jobsWihoutStart.Contains(job))
            {
                jobsWihoutStart.Remove(job);
                ScheduleJob(job.GetJobContext());
            }
        }


        internal abstract void ScheduleJob(JobContext jobContext);

        //Remove job from running jobs
        //And start a new one if it's there
        internal virtual void HandleJobFinished(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    if (dequeuedJobContext.shouldLeave)
                    {
                        //dequeuedJobContext.shouldLeave = false;
                        //dequeuedJobContext.prioritySemaphore.Release();
                    }
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        internal void HandleJobWaiting(JobContext jobContextWaiting, JobContext jobContextWaited)
        {
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContextWaiting);
                if (jobQueue.Count() > 0)
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
        internal virtual void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                Console.WriteLine("PRIOR: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        /*internal void HandleJobPrioritased(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                Console.WriteLine("PRIOR: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
                jobQueue.Enqueue(jobContext);
            }
        }*/

        //Either run job instantly if there's space for it 
        //Or put it in queue
        internal void HandleJobContinueRequested(JobContext jobContext)
        {
            if (jobContext.GetJobState() == JobContext.JobState.Stopped)
            {
                throw new InvalidOperationException("Can't continue a stopped job!");
            }

            lock (schedulerLock)
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

        internal void HandleJobStopped(JobContext jobContext)
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

        internal void HandleJobStartTime(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                ScheduleJob(jobContext);
            }
        }

        internal bool IsPreemptive()
        {
            PriorityQueue pq = jobQueue as PriorityQueue;
            if (pq != null)
            {
                return pq.GetWithPreemption();
            }
            return false;
        }

        internal bool IsTimeSlicing()
        {
            PriorityQueue pq = jobQueue as PriorityQueue;
            if (pq != null)
            {
                return pq.GetWithTimeSlicing();
            }
            return false;
        }
    }
}
