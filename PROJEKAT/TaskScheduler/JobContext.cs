using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Resources;
using System.Runtime.CompilerServices;
using TaskScheduler.Scheduler;

namespace TaskScheduler
{
    //SHOULD BE INTERNAL!!!
    public class JobContext : IJobContext, INotifyPropertyChanged
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
            //In round robin and priority with slicing
            SlicePaused,
            //in priority implementations
            PriorityPaused,
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
        public JobState jobState = JobState.NotStarted;
        private readonly Thread thread;
        internal readonly object jobContextLock = new();
        private readonly Action<JobContext> onJobFinished;
        private readonly Action<JobContext> onJobPaused;
        private readonly Action<JobContext> onJobContinueRequested;
        private readonly Action<JobContext> onJobStopped;
        private readonly Action<JobContext> onJobStarted;
        private readonly Action<JobContext, JobContext> onJobWait;
        private readonly Action<JobContext, ResourceClass> onResourceWanted;
        private readonly Action<JobContext, ResourceClass> onResourceReleased;
        private readonly Action<JobContext> onJobExecution;
        //WAS INIT
        
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

        private static int staticId;
        public int id { get; set; }
        public int Priority { get; set; }

        private double progress;
        //GUI stuff
        public double Progress
        {
            get
            {
                return progress;
            }

            private set
            {
                progress = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsStartable
        {
            get
            {
                lock (jobContextLock)
                {
                    return (jobState == JobState.NotStarted || jobState == JobState.NotScheduled
                            || jobState == JobState.Paused || jobState == JobState.RunningWithPauseRequest);
                }
            }
        }

        public bool IsCloseable
        {
            get
            {
                lock(jobContextLock)
                {
                    return (jobState == JobState.Finished || jobState == JobState.Stopped);
                }
            }
        }

        public bool IsPausable
        {
            get
            {
                lock(jobContextLock)
                {
                    return (jobState == JobState.Running || jobState == JobState.RunningWithPauseRequest);
                }
            }
        }

        public bool IsStoppable
        {
            get
            {
                lock(jobContextLock)
                {
                    return (jobState == JobState.WaitingToResume || jobState == JobState.Running ||
                        jobState == JobState.RunningWithPauseRequest
                        || jobState == JobState.Paused || jobState == JobState.SlicePaused || jobState == JobState.PriorityPaused);
                }
                
            }
        }

        public JobState State
        {
            get => jobState;
            private set
            {
                lock(jobContextLock)
                {
                    jobState = value;
                }
                
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsStartable));
                NotifyPropertyChanged(nameof(IsCloseable));
                NotifyPropertyChanged(nameof(IsStoppable));
                NotifyPropertyChanged(nameof(IsPausable));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
            Action<JobContext, ResourceClass> onResourceWanted,
            Action<JobContext, ResourceClass> onResourceReleased,
            Action<JobContext> onJobExecution,
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
            id = staticId;
            staticId++;
            this.onJobExecution = onJobExecution;
        }

        //SEPARATE PROCESS CONSTRUCTOR, without handlers (no scheduler)
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
                        Finish();
                    }
                }
            });

            Priority = priority;
            StartTime = startTime;
            FinishTime = finishTime;
            MaxExecutionTime = maxExecutionTime;
            this.isSeparate = isSeparate;
            id = staticId;
            staticId++;
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
                        State = JobState.Running;
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
                        State = JobState.Running;
                        resumeSemaphore.Release();
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.PriorityPaused:
                        //start from priority
                        State = JobState.Running;
                        if (shouldWaitForPriority)
                        {
                            shouldWaitForPriority = false;
                            prioritySemaphore.Release();
                        }
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.SlicePaused:
                        //start from slice
                        State = JobState.Running;
                        if (sliced)
                        {
                            sliced = false;
                            sliceSemaphore.Release();
                        }
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.Paused:
                        //release from resource wait
                        State = JobState.Running;
                        
                        /*if(shouldWaitForPriority)
                        {
                            shouldWaitForPriority = false;
                            prioritySemaphore.Release();
                        }*/
                        if(shouldWaitForResource)
                        {
                            shouldWaitForResource = false; 
                            resourceSemaphore.Release();
                        }
                        pauseFinished = DateTime.Now;
                        pauseCheck = true;
                        break;
                    case JobState.Stopped:
                        onJobFinished(this);
                        //State = JobState.Running;
                        //finishedSemaphore.Release();
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
                        break;
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
            //disabling wait for one process -> bugs
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
        public void RequestPause()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        break;
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        State = JobState.RunningWithPauseRequest;
                        break;
                    case JobState.Finished:
                    case JobState.SlicePaused:
                    case JobState.PriorityPaused:
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
        public void RequestContinue()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        break;
                    case JobState.RunningWithPauseRequest:
                        State = JobState.Running;
                        break;
                    case JobState.Running:
                        break;
                    case JobState.Paused:
                        State = JobState.WaitingToResume;
                        if (!isSeparate)
                            onJobContinueRequested(this);
                        else
                        {
                            State = JobState.Running;
                            resumeSemaphore.Release();
                        }
                            
                        break;
                    case JobState.Stopped:
                        if (!isSeparate)
                            onJobContinueRequested(this);
                        break;
                    case JobState.WaitingToResume:
                        State = JobState.WaitingToResume;
                        break;
                    case JobState.Finished:
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        //Same logic for pause, request stoppage and later check it
        public void RequestStop()
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
                    case JobState.SlicePaused:
                    case JobState.PriorityPaused:
                        State = JobState.Stopped;
                        //jobStopped = true;
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
                        State = JobState.Paused;
                        shouldPause = true;
                        pauseStarted = DateTime.Now;
                        if (!isSeparate)
                            onJobPaused(this);
                        break;
                    case JobState.Running:
                    case JobState.WaitingToResume:
                    case JobState.Paused:
                    case JobState.SlicePaused:
                    case JobState.PriorityPaused:
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

        //Call handler if stopped
        public void CheckForStoppage()
        {
            lock (jobContextLock)
            {
                /*if(Priority == 0)
                {
                    Console.WriteLine("CHECKING STOPPAGE: " + jobState);
                }*/
                
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
                    case JobState.SlicePaused:
                    case JobState.PriorityPaused:
                        break;
                    //case JobState.SlicePaused:
                    case JobState.Stopped:
                        /*if(sliced)
                        {
                            sliceSemaphore.Release();
                        }*/
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
        //But it works as follows:
        //if max time was not set, don't do anything
        //Else calculate time from pause to start and add aggregated time from before
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
                //Console.WriteLine("BREAK");
                onJobFinished(this);
                State = JobState.Finished;
                jobState = JobState.Finished;
                return true;
            }
            //Else return false
            return false;
        }

        //If valid finish time was set (didn't stay default), just simple if check
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

        //When checking start  time, there is another thread that loops in a while (bad implementation, but works)
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
            State = js;
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
            // return jobStopped;
            return jobState == JobState.Stopped;
        }

        //just set flag
        internal void RequestPriorityStoppage()
        {
            lock (jobContextLock)
            {
                shouldWaitForPriority = true;
            }
        }

        internal bool shouldWaitForPriority = false;
        //If there was call to wait for priority -> thread has to pause
        public void CheckForPriorityStoppage()
        {            
            lock (jobContextLock)
            {
                if(shouldWaitForPriority)
                {
                    //Console.WriteLine("PRIORITY STOP!");
                    State = JobState.PriorityPaused;
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

        //Same logic as priority -> just set the flag
        internal void RequestSliceStoppage()
        {
            lock (jobContextLock)
            {
                sliced = true;
            }
        }

        internal bool sliced = false;
        //pause if sliced
        public void CheckSliceStoppage()
        {
            lock (jobContextLock)
            {
                if(sliced)
                {
                    //Console.WriteLine("SLICED!");
                    State = JobState.SlicePaused;
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
        //When requesting resource, I have to remember which resource it is, so I can check it later in CheckForResource
        internal void RequestResource(ResourceClass resource)
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

        //Same as single resource, just multiple resources
        private HashSet<ResourceClass> waitedResources = new();
        private bool wantsMoreResources = false;
        internal void RequestResources(HashSet<ResourceClass> resources)
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

        //Call handler
        ResourceClass waitedResource = null;
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
                            /*if(Priority == 1)
                            {
                                Console.WriteLine("AA");
                            }*/
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
                    case JobState.SlicePaused:
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

        //Call release resource handler
        public void ReleaseResource(ResourceClass resource)
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

        //Same logic as for one resource released, just one loop added
        public void ReleaseResources(HashSet<ResourceClass> resources)
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

        //Method made from gui to help
        public void ExecuteJobManually()
        {
            lock(jobContextLock)
            {
                if(jobState == JobState.NotScheduled)
                {
                    onJobExecution(this);
                }
                else
                {
                    throw new Exception("Wrong job to start");
                }
                
            }
        }

        public int GetID()
        {
            return id;
        }

        internal int oldPriority = -1;
        private HashSet<int> priorities = new();


        //When inversing priority, change priority to a new one (which is the same as the other process which called for the resource)
        //And remember old priority, so I can go back to it later
        internal void InversePriority(int newPriority)
        {
            lock(jobContextLock)
            {   if(oldPriority == -1)
                {
                    oldPriority = Priority;
                }
                Priority = newPriority;
            }
        }

        //Get back to old priority when releasing the resource which made problems
        internal void ReversePriority()
        {
            lock(jobContextLock)
            {
                Priority = oldPriority;
                oldPriority = -1;
            }
        }

        //private Action<double> onProgressMade;
        /*internal void SetProgressHandler(Action<double> action)
        {
            onProgressMade = action;
        }*/

        public void SetProgress(double progress)
        {
            Progress = progress;
        }

        public long execTime = 0;
        public void SetJobTime(long time)
        {
            execTime = time;
        }
    }
}
