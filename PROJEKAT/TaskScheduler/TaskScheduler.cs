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
        public int MaxConcurrentTasks { get; set; } = 1;
        private readonly AbstractQueue jobQueue;
        private readonly HashSet<JobContext> runningJobs = new();
        private readonly Dictionary<JobContext, HashSet<JobContext>> mapWaiting = new();
        private readonly HashSet<Job> jobsWihoutStart = new();
        private readonly object schedulerLock = new();
        public static bool isOne = false;

        //If set to true, it's goin to be FIFO
        //Otherwise it's goint to be priority
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
            //Want to disable wait() callouts when there is one job running => better implementation
            if(MaxConcurrentTasks == 1)
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
                onJobWait: HandleJobWaiting);

            //Either start the job if it can be started
            //Or put in in waiting queue
            if (jobSpecification.StartTime < DateTime.Now)
            {
                ScheduleJob(jobContext);
            } else
            {
                jobContext.CheckStartTime();
            }
            

            return new Job(jobContext);
        }

        public Job ScheduleWithStart(JobSpecification jobSpecification)
        {
            return Schedule(jobSpecification); 
        }

        public Job ScheduleWithoutStart(JobSpecification jobSpecification)
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
                onJobWait: HandleJobWaiting);

            Job job = new Job(jobContext);
            jobsWihoutStart.Add(job);

            return job;
        }

        public void ScheduleUnstartedJob(Job job)
        {
            if(jobsWihoutStart.Contains(job))
            {
                jobsWihoutStart.Remove(job);
                ScheduleJob(job.GetJobContext());
            }
        }


        private void ScheduleJob(JobContext jobContext)
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
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                    
                }
            }
        }


        public Job StartJobOnSeparateProcess(JobSpecification jobSpecification)
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
                onJobWait: HandleJobWaiting);

            jobContext.Start();

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
                    if (jobContext.Priority == 1)
                    {
                        Console.WriteLine("YAS!");
                    }
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        /*private void HandleJobWaitingAll(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContext);
                /*if (mapWaiting.ContainsKey(jobContext))
                {
                    HashSet<JobContext> tempValues = new();
                    mapWaiting.TryGetValue(jobContext, out tempValues);
                    foreach(JobContext jb in runningJobs)
                    {
                        if(!tempValues.Contains(jb))
                        {
                            tempValues.Add(jb);
                        }
                    }
                    mapWaiting.Add(jobContext, tempValues);
                }
                else
                {
                    HashSet<JobContext> tempValues = new();
                    tempValues = runningJobs;
                    //tempValues.Add(jobContextWaited);
                    mapWaiting.Add(jobContext, tempValues);
                }
                //HashSet<JobContext> tempValues = new();
                //tempValues = runningJobs;
                //mapWaiting.Add(jobContext, tempValues);



                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }*/

        private void HandleJobWaiting(JobContext jobContextWaiting, JobContext jobContextWaited)
        {
            lock (schedulerLock)
            {
                runningJobs.Remove(jobContextWaiting);
                /*if(mapWaiting.ContainsKey(jobContextWaiting))
                {
                    HashSet<JobContext> tempValues = new();
                    mapWaiting.TryGetValue(jobContextWaiting, out tempValues);
                    if(!tempValues.Contains(jobContextWaited))
                    {
                        tempValues.Add(jobContextWaited);
                    }
                    mapWaiting.Add(jobContextWaiting, tempValues);
                } else
                {
                    HashSet<JobContext> tempValues = new();
                    tempValues.Add(jobContextWaited);
                    mapWaiting.Add(jobContextWaiting, tempValues);
                }*/
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
            }
        }

        //I'd implement this without a semaphore, but rather as a pause and continue
        //Need to finish this implementation and test it for good
        /*private void HandleJobRelease(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                Dictionary<JobContext, HashSet<JobContext>>.ValueCollection values = mapWaiting.Values;
                //Check for all values (sets)
                for (int i = 0; i < values.Count; i++)
                {
                    //If map contains a set (which should always be true)
                    if (mapWaiting.ContainsValue(values.ElementAt(i)))
                    {
                        //If set contains a job that finished it's work (someone was waiting on that job)
                        //Remove it from the list
                        if(values.ElementAt(i).Contains(jobContext))
                        {
                            values.ElementAt(i).Remove(jobContext);
                        }
                        //After we removed that job, it's time to release job that was waiting
                        if(values.ElementAt(i).Count == 0)
                        {
                            JobContext key = null;
                            foreach(var type in mapWaiting)
                            {
                                if(type.Value == values.ElementAt(i))
                                {
                                    //Get a key (jobContext) for a specific set
                                    key = type.Key;
                                }
                            }
                            if(key != null)
                            {
                                //Handle it as a continue request
                                HandleJobContinueRequested(key);
                            }
                        }
                    }
                }
            }
        }*/

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
                throw new InvalidOperationException("Can't continue a stopped job!");
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

        private void HandleJobStartTime(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                ScheduleJob(jobContext);
            }
        }

    }
}
