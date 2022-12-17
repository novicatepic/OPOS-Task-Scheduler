using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public abstract class AbstractScheduler
    {
        public int MaxConcurrentTasks { get; set; } = 1;
        protected AbstractQueue jobQueue;
        protected Dictionary<JobContext, HashSet<Resource>> resourceMap = new();
        private Dictionary<JobContext, HashSet<JobContext>> whoHoldsResources = new();
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
            JobContext jobContext = CreateJobContext(jobSpecification);

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

        private JobContext CreateJobContext(JobSpecification jobSpecification)
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
                            onResourceWanted: HandleResourceWanted,
                            isSeparate: false);
            return jobContext;
        }

        public Job AddJobWithScheduling(JobSpecification jobSpecification)
        {
            return Schedule(jobSpecification);
        }

        public Job AddJobWithoutScheduling(JobSpecification jobSpecification)
        {
            JobContext jobContext = CreateJobContext(jobSpecification);

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
                Console.WriteLine("PRIOR:2 " + jobContext.Priority);
                if (resourceMap.ContainsKey(jobContext))
                {
                    resourceMap.Remove(jobContext);
                }
                if (whoHoldsResources.ContainsKey(jobContext))
                {
                    Console.WriteLine("KNOCK!");
                    HashSet<JobContext> jb = whoHoldsResources[jobContext];
                    foreach (var element in jb)
                    {
                        element.Start();
                    }

                    whoHoldsResources.Remove(jobContext);
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

        internal void HandleResourceWanted(JobContext jobContext, Resource resource)
        {
            bool someoneHoldingResource = false;
            lock (schedulerLock)
            {
                Console.WriteLine("ENTERED");
                //lock(jobContext.jobContextLock)
                //{
                foreach (var element in resourceMap)
                {
                    if (element.Value != null && element.Value.Contains(resource))
                    {
                        someoneHoldingResource = true;
                        break;
                    }
                }
                if (someoneHoldingResource)
                {
                    Console.WriteLine("RWAIT");
                    for (int i = 0; i < resourceMap.Count; i++)
                    {
                        if (resourceMap.ElementAt(i).Value.Contains(resource))
                        {
                            Console.WriteLine("HOLDS");
                            if (!whoHoldsResources.ContainsKey(resourceMap.ElementAt(i).Key))
                            {
                                //HashSet<JobContext> lmao = new();
                                whoHoldsResources.Add(resourceMap.ElementAt(i).Key, new HashSet<JobContext>());
                            }
                            whoHoldsResources[resourceMap.ElementAt(i).Key].Add(jobContext);
                            Console.WriteLine("PRIOR: " + resourceMap.ElementAt(i).Key.Priority);
                            break;
                        }
                    }

                }
                else
                {
                    Console.WriteLine("RGOTIT");
                    if (!resourceMap.ContainsKey(jobContext))
                    {
                        resourceMap.Add(jobContext, new HashSet<Resource>());
                        HashSet<Resource> gotSet = new();
                        resourceMap[jobContext].Add(resource);
                    }
                    else
                    {
                        HashSet<Resource> gotSet = new();
                        resourceMap.TryGetValue(jobContext, out gotSet);
                        if (gotSet.Contains(resource))
                        {
                            throw new InvalidOperationException("Can't request a resource you are already holding!");
                        }
                        resourceMap[jobContext].Add(resource);
                    }
                }

                //}
            }

            DeadlockDetectionGraph graph = MakeDetectionGraph();
            graph.PrintMatrix();

            if (someoneHoldingResource)
            {
                Console.WriteLine("WAIT!");
                jobContext.SetJobState(JobContext.JobState.Paused);
                jobContext.shouldWaitForResource = true;
            }

        }


        //DeadlockDetectionGraph deadlockDetectionGraph = new();
        private DeadlockDetectionGraph MakeDetectionGraph()
        {
            lock (schedulerLock)
            {
                var graphSize = runningJobs.Count;

                DeadlockDetectionGraph deadlockDetectionGraph = new(graphSize);

                //INITIALIZATION
                for (int i = 0; i < graphSize; i++)
                {
                    deadlockDetectionGraph.nodes[i] = runningJobs.ElementAt(i).GetID();
                }

                for (int i = 0; i < graphSize; i++)
                {
                    for (int j = 0; j < graphSize; j++)
                    {
                        if (i != j)
                        {
                            if(whoHoldsResources.ContainsKey(runningJobs.ElementAt(i)))
                            {
                                foreach(var element in whoHoldsResources[runningJobs.ElementAt(i)])
                                {
                                    int position = deadlockDetectionGraph.FindPositionOfState(element.GetID());
                                    deadlockDetectionGraph.ms[i, j] = 1;
                                }
                            }
                            else
                            {
                                deadlockDetectionGraph.ms[i, j] = 0;
                            }
                        }
                        else
                        {
                            deadlockDetectionGraph.ms[i, j] = 0;
                        }
                    }
                }

                return deadlockDetectionGraph;
            }
        }



        //private void

    }
}
