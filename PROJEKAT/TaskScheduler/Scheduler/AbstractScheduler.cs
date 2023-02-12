using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaskScheduler.Graph;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public abstract class AbstractScheduler
    {
        public int MaxConcurrentTasks { get; set; } = 1;
        //Jobs on wait
        internal AbstractQueue? jobQueue;
        //Resources held by particular Job(Context)
        public Dictionary<JobContext, HashSet<ResourceClass>> resourceMap = new();
        //Who waits on who
        public Dictionary<JobContext, HashSet<JobContext>> whoHoldsResources = new();
        //Who waits on what
        internal Dictionary<JobContext, HashSet<ResourceClass>> jobWaitingOnResources = new();
        //Who is running
        internal readonly HashSet<JobContext> runningJobs = new();
        //Who is waiting to be started explicitly
        public readonly HashSet<Job> jobsWihoutStart = new();
        //Who is a potential candidate to form a deadlock (graph maker)
        protected readonly HashSet<JobContext> deadlockJobs = new();

        public readonly object schedulerLock = new();
        public static bool isOne = false;

        //This caused me to break all OOP and SOLID principles :)
        public ObservableCollection<JobContext> guiJobs = new();

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
            lock (schedulerLock)
            {
                guiJobs.Add(jobContext);
            }
            if (jobSpecification.StartTime < DateTime.Now)
            {
                //lock(schedulerLock)
                //{
                //   guiJobs.Add(jobContext);
                //}
                ScheduleJob(jobContext);
            }
            else
            {
                jobContext.CheckStartTime();
            }

            Job job = new Job(jobContext);
            return job;
        }

        //Simple func
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
                            onResourceReleased: HandleResourceReleased,
                            onJobExecution: HandleJobExecution,
                            isSeparate: false);
            return jobContext;
        }

        public Job AddJobWithScheduling(JobSpecification jobSpecification)
        {
            return Schedule(jobSpecification);
        }

        //Add job in a queue that's not going to start yet
        //Because it will potentially be started later 
        //Also set state to a newly made state -> NotScheduled
        public Job AddJobWithoutScheduling(JobSpecification jobSpecification)
        {
            JobContext jobContext = CreateJobContext(jobSpecification);

            jobContext.SetJobState(JobContext.JobState.NotScheduled);

            Job job = new Job(jobContext);
            jobsWihoutStart.Add(job);

            lock(schedulerLock)
            {
                if (!guiJobs.Contains(jobContext))
                {
                    guiJobs.Add(jobContext);
                }
            }

            return job;
        }

        //Maybe should throw an exception if an user makes a mistake, but doesn't really matter
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

        internal virtual void HandleResourceReleased(JobContext jobContext, ResourceClass resource)
        {
            lock(schedulerLock)
            {
                if(resourceMap.ContainsKey(jobContext))
                {
                    //If the job holds the resource
                    if (resourceMap[jobContext].Contains(resource))
                    {
                        //Remove resource from that job
                        resourceMap[jobContext].Remove(resource);
                        //For each job that waits on that resource which is being released
                        foreach(var job in jobWaitingOnResources)
                        {
                            if(job.Value.Contains(resource))
                            {
                                //Job doesn't wait on that resource anymore and that job can continue to work :)
                                //But only if he doesn't wait for anything else
                                job.Value.Remove(resource);
                                if(job.Value.Count == 0 && runningJobs.Count < MaxConcurrentTasks)
                                {
                                    if(deadlockJobs.Contains(job.Key))
                                    {
                                        deadlockJobs.Remove(job.Key);
                                    }
                                    //Give the resource to the corresponding job
                                    if(!resourceMap.ContainsKey(job.Key))
                                    {
                                        resourceMap.Add(job.Key, new HashSet<ResourceClass>());
                                        resourceMap[job.Key].Add(resource);
                                        Console.WriteLine("ADDED");
                                    }
                                    else
                                    {
                                        Console.WriteLine("ADDED");
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    if(jobWaitingOnResources.ContainsKey(jobContext))
                                    {
                                        jobWaitingOnResources[jobContext].Remove(resource);
                                    }
                                    
                                    runningJobs.Add(job.Key);
                                    job.Key.Start();
                                }
                                else if(job.Value.Count == 0)
                                {
                                    if (!resourceMap.ContainsKey(job.Key))
                                    {
                                        resourceMap.Add(job.Key, new HashSet<ResourceClass>());
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    else
                                    {
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    if (jobWaitingOnResources.ContainsKey(jobContext))
                                    {
                                        jobWaitingOnResources[jobContext].Remove(resource);
                                    }
                                    jobQueue.Enqueue(job.Key, job.Key.Priority);
                                }
                                whoHoldsResources[jobContext].Remove(job.Key);
                                break;
                            }
                        }
                    } 
                    else
                    {
                        throw new InvalidOperationException("Can't release a resource which is not held by the job!");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Can't release a resource which is not held by the job!");
                }
            }
        }

        internal virtual void HandleJobFinished(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                jobContext.SetJobState(JobContext.JobState.Finished);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
                //Release job from resource map -> it's dead now
                HashSet<ResourceClass> resources = new();
                if (resourceMap.ContainsKey(jobContext))
                {
                    resources = resourceMap[jobContext];
                    resourceMap.Remove(jobContext);
                }
                if (whoHoldsResources.ContainsKey(jobContext))
                {
                    //GET WHICH JOBS TO RELEASE
                    foreach(var element in jobWaitingOnResources)
                    {
                        bool isZero = false;
                        foreach(var resource in resources)
                        {
                            if(element.Value.Contains(resource))
                            {
                                element.Value.Remove(resource);
                                if (element.Value.Count == 0) isZero = true;
                            }
                        }
                        //If job is not waiting for anything else, start it because it should be started
                        if(isZero && runningJobs.Count < MaxConcurrentTasks)
                        {
                            runningJobs.Add(element.Key);
                            element.Key.Start();
                        }
                        //Else if queue is full, add it to runningJobs
                        else if(isZero)
                        {
                            jobQueue.Enqueue(element.Key, element.Key.Priority);
                        }
                    }

                    whoHoldsResources.Remove(jobContext);
                }
            }
        }

        //Not necessary, thought it should be implemented this way
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
                //Console.WriteLine("PRIOR: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    //Console.WriteLine("PRIOR DEQUEUED: " + dequeuedJobContext.Priority);
                    dequeuedJobContext.Start();
                }
            }
        }

        //Either run job instantly if there's space for it 
        //Or put it in queue
        internal virtual void HandleJobContinueRequested(JobContext jobContext)
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
                if(runningJobs.Contains(jobContext))
                {
                    runningJobs.Remove(jobContext);
                }

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

        internal void HandleJobExecution(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                ScheduleJob(jobContext);
            }
        }

        internal virtual void HandleResourceWanted(JobContext jobContext, ResourceClass resource)
        {

            lock (schedulerLock)
            {
                
                bool someoneHoldingResource = false;
                foreach (var element in resourceMap)
                {
                    if (element.Value != null && element.Value.Contains(resource))
                    {
                        //Found that someone is holding the resource
                        someoneHoldingResource = true;
                        break;
                    }
                }
                //IF SOMEONE IS HOLDING WANTED RESOURCE
                //ADD JOB CONTEXT ON WAITING QUEUE
                if (someoneHoldingResource)
                {
                    for (int i = 0; i < resourceMap.Count; i++)
                    {
                        if (resourceMap.ElementAt(i).Value.Contains(resource))
                        {
                            if (!whoHoldsResources.ContainsKey(resourceMap.ElementAt(i).Key))
                            {
                                whoHoldsResources.Add(resourceMap.ElementAt(i).Key, new HashSet<JobContext>());
                            }
                            whoHoldsResources[resourceMap.ElementAt(i).Key].Add(jobContext);
                            break;
                        }
                    }

                }
                else
                {
                    //ELSE JOBCONTEXT CAN GET THE RESOURCE FREELY
                    if (!resourceMap.ContainsKey(jobContext))
                    {
                        resourceMap.Add(jobContext, new HashSet<ResourceClass>());
                        HashSet<ResourceClass> gotSet = new();
                        resourceMap[jobContext].Add(resource);
                    }
                    else
                    {
                        HashSet<ResourceClass> gotSet = new();
                        resourceMap.TryGetValue(jobContext, out gotSet);
                        if (gotSet.Contains(resource))
                        {
                            throw new InvalidOperationException("Can't request a resource you are already holding!");
                        }
                        resourceMap[jobContext].Add(resource);
                    }
                }

                DeadlockDetectionGraph graph = MakeDetectionGraph();
                //Console.WriteLine("Matrix print!");
                //graph.PrintMatrix();

                bool cycleFound = graph.DFSForCycleCheck(jobContext.GetID());

                //PREVENT EVENTUAL CYCLE PROBLEMS WTIH cycleFound in if-condition
                //IF JOB WAS RUNNING AND WANTED RESORUCE THAT IS HELD -> PAUSE IT
                if (someoneHoldingResource && cycleFound == false)
                {
                    //Adding next four lines of code so I can remember who is waiting on what
                    if(!jobWaitingOnResources.ContainsKey(jobContext))
                    {
                        jobWaitingOnResources.Add(jobContext, new HashSet<ResourceClass>());
                    }
                    jobWaitingOnResources[jobContext].Add(resource);
                    //Pause the job and lock the semaphore
                    jobContext.SetJobState(JobContext.JobState.Paused);
                    deadlockJobs.Add(jobContext);
                    //Console.WriteLine("PAUSED!");
                    jobContext.shouldWaitForResource = true;
                    //It's basically not in running jobs anymore
                    runningJobs.Remove(jobContext);
                    //Check if there is another job to start since this job is paused
                    if(jobQueue.Count() > 0 && runningJobs.Count < MaxConcurrentTasks)
                    {
                        JobContext jb = jobQueue.Dequeue();
                        runningJobs.Add(jb);
                        jb.Start();
                    }
                }
                //Prolly should just throw an exception
                else if(cycleFound == true)
                {
                    Console.WriteLine("Resource not allowed, deadlock would be caused!");

                    //Or eventually throw exception to make life easier
                    //Or give the resource to the job that wants it so that multiple processes can share the resource
                    //Or kill the process

                    //jobContext.RequestStop();
                    Console.WriteLine("REQUEST STOPPAGE");
                    HashSet<ResourceClass> resources = resourceMap[jobContext];
                    foreach(var res in resources)
                    {
                        HandleResourceReleased(jobContext, res);
                    }
                    jobContext.RequestStop();
                }
            }
        }

        //To make a graph, I have to consider running jobs and jobs waiting on resources
        //So if there is a cycle I can correct the mistake up there
        protected DeadlockDetectionGraph MakeDetectionGraph()
        {
            lock (schedulerLock)
            {
                var graphSize = runningJobs.Count + deadlockJobs.Count;

                DeadlockDetectionGraph deadlockDetectionGraph = new(graphSize);

                //INITIALIZATION
                int k = 0;
                for (int i = 0; i < runningJobs.Count; i++)
                {
                    deadlockDetectionGraph.nodes[i] = runningJobs.ElementAt(i).GetID();
                    ++k;
                }
                for (int i = 0; i < deadlockJobs.Count; i++)
                {
                    deadlockDetectionGraph.nodes[k++] = deadlockJobs.ElementAt(i).GetID();
                }
                //GET  GRAPH MAPPINGS TO CHECK IF THERE IS A POTENTIAL CYCLE
                for (int i = 0; i < graphSize; i++)
                {
                    if (i < runningJobs.Count && whoHoldsResources.ContainsKey(runningJobs.ElementAt(i)))
                    {
                        foreach (var element in whoHoldsResources[runningJobs.ElementAt(i)])
                        {
                            int position = deadlockDetectionGraph.FindPositionOfState(element.GetID());
                            //Console.WriteLine("ENTERED!");
                            if(position != -1)
                            {
                                deadlockDetectionGraph.ms[i, position] = 1;
                            }
                            
                        }
                    }
                    if (i < deadlockJobs.Count &&  whoHoldsResources.ContainsKey(deadlockJobs.ElementAt(i)))
                    {
                        foreach (var element in whoHoldsResources[deadlockJobs.ElementAt(i)])
                        {
                            int position = deadlockDetectionGraph.FindPositionOfState(element.GetID());
                            //Console.WriteLine("ENTERED!");
                            if (position != -1)
                            {
                                deadlockDetectionGraph.ms[i, position] = 1;
                            }

                        }
                    }
                }

                return deadlockDetectionGraph;
            }
        }

    }
}
