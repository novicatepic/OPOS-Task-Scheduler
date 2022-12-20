using System.Resources;
using TaskScheduler.Scheduler;

namespace TaskScheduler
{
    //SHOULD BE INTERNAL!!!
    public class JobContext : IJobContext
    {
        //Concerning job, each job has it's own state
        //Changed to internal
        public enum JobState
        {
            NotScheduled,
            NotStarted,
            Running,
            RunningWithPauseRequest,
            WaitingToResume,
            Paused,
            Stopped,
            Finished
        }


        private int numWaited = 0;
        private bool waited = false;
        private bool isSeparate = false;
        internal DateTime StartTime { get; init; }
        internal DateTime FinishTime { get; init; }
        internal int MaxExecutionTime { get; init; }
        private JobState jobState = JobState.NotStarted;
        private readonly Thread thread;
        internal readonly object jobContextLock = new();
        private readonly Action<JobContext> onJobFinished;
        private readonly Action<JobContext> onJobPaused;
        private readonly Action<JobContext> onJobContinueRequested;
        private readonly Action<JobContext> onJobStopped;
        private readonly Action<JobContext> onJobStarted;
        private readonly Action<JobContext, JobContext> onJobWait;
        private readonly Action<JobContext, Resource> onResourceWanted;
        private readonly Action<JobContext, Resource> onResourceReleased;
        //WAS INIT
        internal int Priority { get; set; }
        private static readonly SemaphoreSlim finishedSemaphore = new(0);
        private static readonly SemaphoreSlim waitOnOtherJobSemaphore = new(0);
        private readonly SemaphoreSlim resumeSemaphore = new(0);
        private static int numWaiters = 0;                      //static necessary!
        private IUserJob userJob;
        private bool jobStopped = false;
        internal DateTime tempTime;      //in case if user decided to specify MaxExecution time
        internal bool shouldLeave = false;
        internal readonly SemaphoreSlim prioritySemaphore = new(0);
        internal readonly SemaphoreSlim sliceSemaphore = new(0);


        private static int staticId = 0;
        private int id = 0;
        public JobContext(IUserJob userJob, int priority,
            DateTime startTime,
            DateTime finishTime,
            int maxExecutionTime,
            Action<JobContext> onJobFinished,
            Action<JobContext> onJobPaused,
            Action<JobContext> onJobContinueRequested,
            Action<JobContext> onJobStopped,
            Action<JobContext> onJobStarted,
            Action<JobContext, JobContext> onJobWait,
            Action<JobContext, Resource> onResourceWanted,
            Action<JobContext, Resource> onResourceReleased,
            bool isSeparate)
        {
            this.userJob = userJob;
            thread = new(() =>
            {
                //Calls Run method from and Finishes, but it's not started yet, only declaring what thread will do
                try
                {
                    userJob.Run(this);
                }
                finally
                {
                    if (!(jobState == JobState.Stopped))
                    {
                        Finish();
                    }
                }
            });

            Priority = priority;
            this.onJobFinished = onJobFinished;
            this.onJobPaused = onJobPaused;
            this.onJobContinueRequested = onJobContinueRequested;
            this.onJobStopped = onJobStopped;
            this.onJobStarted = onJobStarted;
            this.onJobWait = onJobWait;
            this.onResourceWanted = onResourceWanted;
            this.onResourceReleased = onResourceReleased;
            StartTime = startTime;
            FinishTime = finishTime;
            MaxExecutionTime = maxExecutionTime;
            this.isSeparate = isSeparate;
            id = staticId++;
        }

        //SEPARATE PROCESS CONSTRUCTOR
        public JobContext(IUserJob userJob, int priority, DateTime startTime, DateTime finishTime, int maxExecutionTime, bool isSeparate)
        {
            this.userJob = userJob;
            thread = new(() =>
            {
                //Calls Run method from and Finishes, but it's not started yet, only declaring what thread will do
                try
                {
                    userJob.Run(this);
                }
                finally
                {
                    if (!(jobState == JobState.Stopped))
                    {
                        //Console.WriteLine("YES!");
                        Finish();
                    }
                }
            });

            Priority = priority;
            StartTime = startTime;
            FinishTime = finishTime;
            MaxExecutionTime = maxExecutionTime;
            this.isSeparate = isSeparate;
        }

        //Start() is either going to start the job
        //Or it's going to resume the job if it was paused before
        //private DateTime timeContinue = new DateTime(2010, 1, 1);
        private bool firstStart = false;
        private DateTime pauseFinished = new DateTime(2010, 1, 1);
        private bool pauseCheck = false;
        internal void Start()
        {
            if(!firstStart)
            {
                tempTime = DateTime.Now;
            }
            
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotScheduled:
                    case JobState.NotStarted:
                        CheckStartTime();
                        jobState = JobState.Running;
                        //When job starts, thread is started, and we know what thread does, it causes mayhem :)
                        thread.Start();
                        break;
                    case JobState.RunningWithPauseRequest:
                        break;
                    case JobState.Running:
                        break;  //TESTING PURPOSES
                        //throw new InvalidOperationException("Job already started");
                    case JobState.Finished:
                        throw new InvalidOperationException("Job already finished");
                    case JobState.WaitingToResume:
                        jobState = JobState.Running;
                        resumeSemaphore.Release();
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.Paused:
                        jobState = JobState.Running;
                        if (sliced)
                        {
                            sliced = false;
                            sliceSemaphore.Release();
                        }
                        if(shouldWaitForPriority)
                        {
                            shouldWaitForPriority = false;
                            prioritySemaphore.Release();
                        }
                        if(shouldWaitForResource)
                        {
                            shouldWaitForResource = false; 
                            resourceSemaphore.Release();
                        }
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.Stopped:
                        jobState = JobState.Running;
                        finishedSemaphore.Release();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        //Finish() is private method and it doesn't make sense for us to call it
        //If there were threads waiting they are going to be set free
        //And onJobFinished logic is going to be implemented
        //private static int waitReleaser = 0;
        private void Finish()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Job not started");
                    case JobState.Paused:
                        break;
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                    case JobState.Stopped:
                        if (numWaiters > 0)
                        {
                            finishedSemaphore.Release(numWaiters);
                        }

                        if (waited)
                        {
                            waited = false;
                            waitOnOtherJobSemaphore.Release(numWaited);
                        }
                        if(!isSeparate) 
                        {
                            onJobFinished(this);
                        }
                        
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Job already finished.");
                    /*case JobState.Stopped:
                        throw new InvalidOperationException("Job stopped.");*/
                    default:
                        throw new InvalidOperationException("Invalid job state");

                }
            }
        }

        //Thread doesn't run anymore but it waits (semaphore) and increases numWaiters so they can be released
        internal void Wait()
        {
            if (!AbstractScheduler.isOne)
            {
                lock (jobContextLock)
                {
                    switch (jobState)
                    {
                        case JobState.NotStarted:
                        case JobState.RunningWithPauseRequest:
                        case JobState.Running:
                            numWaiters++;
                            
                            //finishedSemaphore.Wait();
                            break;
                        case JobState.Finished:
                            return;
                        default:
                            throw new InvalidOperationException("Invalid job state");
                    }
                }
                finishedSemaphore.Wait();
            }
        }

        internal void Wait(JobContext job)
        {
            lock (jobContextLock)
            {
                if (job.jobState == JobState.Running || job.jobState == JobState.RunningWithPauseRequest)
                {
                    switch (jobState)
                    {
                        case JobState.NotStarted:
                        case JobState.RunningWithPauseRequest:
                        case JobState.Running:
                            job.waited = true;
                            onJobWait(this, job);
                            job.numWaited++;
                            waitOnOtherJobSemaphore.Wait();
                            break;
                        case JobState.Finished:
                            return;
                        default:
                            throw new InvalidOperationException("Invalid job state");
                    }
                } else
                {
                    throw new InvalidOperationException("Can't call wait on job that is not active!");
                }
                

            }

        }

        //When we request pause, we don't go into pause mode straight away
        //But rather we update the state which will later be checked by CheckPause() method
        internal void RequestPause()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        break;
                    case JobState.RunningWithPauseRequest:
                        break;
                    case JobState.Running:
                        jobState = JobState.RunningWithPauseRequest;
                        break;
                    case JobState.Finished:
                        return;
                    case JobState.Paused:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        //If job was paused
        //It only makes sense to wait for resuming, and calls onJobContinueRequested
        //Wouldn't make sense to go into running state because other jobs are being worked on
        internal void RequestContinue()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        break;
                    case JobState.RunningWithPauseRequest:
                        jobState = JobState.Running;
                        break;
                    case JobState.Running:
                        break;
                    case JobState.Paused:
                        jobState = JobState.WaitingToResume;
                        if (!isSeparate)
                            onJobContinueRequested(this);
                        else
                        {
                            jobState = JobState.Running;
                            resumeSemaphore.Release();
                        }
                            
                        break;
                    case JobState.Stopped:
                        if (!isSeparate)
                            onJobContinueRequested(this);
                        break;
                    case JobState.WaitingToResume:
                        jobState = JobState.WaitingToResume;
                        break;
                    case JobState.Finished:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        internal void RequestStop()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Job not started!");
                    case JobState.Finished:
                        throw new InvalidOperationException("Job finished!");    //I can use break as well here
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                    case JobState.WaitingToResume:
                    case JobState.Paused:
                        jobState = JobState.Stopped;
                        jobStopped = true;
                        break;
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");

                }
            }
        }

        //Job is always going to check for a pause
        //After it has finished part of the job, and if pause was requested
        //Job goes into pause mode and waits (semaphore)
        private DateTime pauseStarted = new DateTime(2010, 1, 1);
        public void CheckForPause()
        {
            bool shouldPause = false;
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.RunningWithPauseRequest:
                        jobState = JobState.Paused;
                        shouldPause = true;
                        pauseStarted = DateTime.Now;
                        if (!isSeparate)
                            onJobPaused(this);
                        break;
                    case JobState.Running:
                    case JobState.WaitingToResume:
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }

            if (shouldPause)
            {
                pauseStarted = DateTime.Now;
                resumeSemaphore.Wait();
            }
        }

        public void CheckForStoppage()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        break;
                    case JobState.RunningWithPauseRequest:
                        break;
                    case JobState.Running:
                        break;
                    case JobState.Finished:
                    case JobState.WaitingToResume:
                    case JobState.Paused:
                        break;
                    case JobState.Stopped:
                        //jobState = JobState.Finished;
                        if(!isSeparate)
                            onJobStopped(this);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        //PROBABLY THERE IS A BETTER IMPLEMENTATION, AND THAT IS TO CHECK TIME SOMEWHERE INTERNALLY, THIS IS NOT PRECISE ENOUGH
        private double differenceInMilliseconds = 0;
        
        public bool CheckExecutionTime()
        {
            double aggregatedPauseTime = 0;
            //If user didn't set max execution time, we don't really care
            if (MaxExecutionTime == 0)
            {
                return false;
            }
            //If execution time is longer that specified, it's time to stop
            TimeSpan ts = DateTime.Now - tempTime;
            //Console.WriteLine("MS DIFFERENCE: " + differenceInMilliseconds);
            differenceInMilliseconds = ts.TotalMilliseconds;
            if(pauseCheck)
            {
                pauseCheck = false;
                TimeSpan ts2 = pauseFinished - pauseStarted;
                aggregatedPauseTime += ts2.TotalMilliseconds;
            }
            differenceInMilliseconds -= aggregatedPauseTime;
            //If job worked longer than it should have work
            if (differenceInMilliseconds >= MaxExecutionTime)
            {
                return true;
            }
            //Else return false
            return false;
        }

        public bool CheckFinishTime()
        {
            //Base cases
            if (FinishTime.Year == 2010 || FinishTime.Year < DateTime.Now.Year || FinishTime.Month < DateTime.Now.Month || FinishTime.Hour < DateTime.Now.Hour
                || FinishTime.Day < DateTime.Now.Day)
            {
                return false;
            }
            //If it still has time to finish
            if (DateTime.Now < FinishTime)
            {
                return false;
            }
            //Else it was running for too long
            return true;
        }

        public void CheckStartTime()
        {
            //If it stayed default or user specified a date in the past
            if (StartTime.Year == 2010 || StartTime < DateTime.Now)
            {
                return;
            }
            Thread helpThread;
            helpThread = new(() =>
            {
                while (StartTime > DateTime.Now) { }
                onJobStarted(this);
            });
            helpThread.Start();
        }

        internal JobState GetJobState()
        {
            return jobState;
        }

        internal void SetJobState(JobState js)
        {
            jobState = js;
        }

        internal int GetPriority()
        {
            return Priority;
        }

        internal IUserJob GetUserJob()
        {
            return this.userJob;
        }

        public bool StoppageConfirmed()
        {
            return jobStopped;
        }

        internal void RequestPriorityStoppage()
        {
            lock (jobContextLock)
            {
                shouldWaitForPriority = true;
            }
        }

        internal bool shouldWaitForPriority = false;
        public void CheckForPriorityStoppage()
        {            
            lock (jobContextLock)
            {
                if(shouldWaitForPriority)
                {
                    jobState = JobState.Paused;
                    if(!isSeparate)
                    {
                        onJobPaused(this);
                    }
                }
            }

            if (shouldWaitForPriority)
            {
                prioritySemaphore.Wait();
            }
        }

        internal void RequestSliceStoppage()
        {
            lock (jobContextLock)
            {
                sliced = true;
            }
        }

        internal bool sliced = false;
        public void CheckSliceStoppage()
        {
            lock (jobContextLock)
            {
                if(sliced)
                {
                    jobState = JobState.Paused;
                    if (!isSeparate)
                    {
                        onJobPaused(this);
                    }
                }
            }

            if (sliced)
            {
                sliceSemaphore.Wait();
            }
        }

        internal int sliceTime = 0;
        internal void SetSliceTime(int sliceTime)
        {
            this.sliceTime = sliceTime * 1000;      //convert it to ms
        }

        internal SemaphoreSlim resourceSemaphore = new SemaphoreSlim(0);
        internal bool shouldWaitForResource = false;
        private bool wantsResourse = false;
        internal void RequestResource(Resource resource)
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotScheduled:
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Requesting a resource when not started.");
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        //onResourceWanted(this, resource);
                        waitedResource = resource;
                        wantsResourse = true;
                        break;
                    case JobState.WaitingToResume:
                        break;
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        private HashSet<Resource> waitedResources = new();
        private bool wantsMoreResources = false;
        internal void RequestResources(HashSet<Resource> resources)
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotScheduled:
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Requesting a resource when not started.");
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        foreach(var resource in resources)
                        {
                            waitedResources.Add(resource);
                        }
                        wantsMoreResources = true;
                        break;
                    case JobState.WaitingToResume:
                        break;
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        Resource waitedResource = null;
        public void CheckForResourse()
        {
            //bool shouldPause = false;
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        //jobState = JobState.Paused;
                        if(!isSeparate && wantsResourse)
                        {
                            if(Priority == 1)
                            {
                                Console.WriteLine("AA");
                            }
                            wantsResourse = false;
                            onResourceWanted(this, waitedResource);
                        }
                        if(!isSeparate && wantsMoreResources)
                        {
                            wantsMoreResources = false;
                            foreach(var element in waitedResources)
                            {
                                onResourceWanted(this, element);
                            }
                            waitedResources.Clear();
                        }
                        break;
                    case JobState.WaitingToResume:
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }

            if (shouldWaitForResource)
            {
                resourceSemaphore.Wait();
            }
        }

        public void ReleaseResource(Resource resource)
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotScheduled:
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Requesting release when not possible.");
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        onResourceReleased(this, resource);
                        break;
                    case JobState.WaitingToResume:
                        break;
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        public void ReleaseResources(HashSet<Resource> resources)
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotScheduled:
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Requesting release when not possible.");
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        foreach (var r in resources)
                        {
                            onResourceReleased(this, r);
                        }
                        break;
                    case JobState.WaitingToResume:
                        break;
                    case JobState.Paused:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.Stopped:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        public int GetID()
        {
            return id;
        }

        internal int oldPriority = -1;
        private HashSet<int> priorities = new();
        internal void InversePriority(int newPriority)
        {
            lock(jobContextLock)
            {   if(oldPriority == -1)
                {
                    oldPriority = Priority;
                }
                //priorities.Push(newPriority);
                //priorities.Add(newPriority);
                Priority = newPriority;
            }
        }

        internal void ReversePriority()
        {
            lock(jobContextLock)
            {
                Priority = oldPriority;
                oldPriority = -1;
                /*priorities.Remove(whichPriority);
                if(whichPriority == Priority && priorities.Count != 0)
                {
                    Priority = priorities.ElementAt(priorities.Count - 1);
                }
                if(priorities.Count == 0)
                {
                    Priority = oldPriority;
                    oldPriority = -1;
                }*/
 
            }
        }

    }
}
