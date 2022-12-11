using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    internal class JobContext : IJobContext
    {
        //Concerning job, each job has it's own state
        //Changed to internal
        public enum JobState
        {
            NotStarted,
            Running,
            RunningWithPauseRequest,
            WaitingToResume,
            Paused,
            Blocked,    //waiting for other task(s) to finish their job => equivalent to wait(), notify() in java
            Stopped,
            Finished
        }


        private static int id;
        private bool waited = false;

        internal DateTime StartTime { get; init; }
        internal DateTime FinishTime { get; init; }
        internal int MaxExecutionTime { get; init; }
        private JobState jobState = JobState.NotStarted;
        private readonly Thread thread;
        private readonly object jobContextLock = new();
        private readonly Action<JobContext> onJobFinished;
        private readonly Action<JobContext> onJobPaused;
        private readonly Action<JobContext> onJobContinueRequested;
        private readonly Action<JobContext> onJobStopped;
        private readonly Action<JobContext> onJobStarted;
        internal int Priority { get; init; }
        private static readonly SemaphoreSlim finishedSemaphore = new(0);
        private readonly SemaphoreSlim resumeSemaphore = new(0);
        private static int numWaiters = 0;                      //static necessary!
        private IUserJob userJob;
        private bool jobStopped = false;
        private DateTime tempTime;      //in case if user decided to specify MaxExecution time, withouth start or finish time
        public JobContext(IUserJob userJob, int priority,
            DateTime startTime,
            DateTime finishTime,
            int maxExecutionTime,
            Action<JobContext> onJobFinished,
            Action<JobContext> onJobPaused,
            Action<JobContext> onJobContinueRequested,
            Action<JobContext> onJobStopped,
            Action<JobContext> onJobStarted)
        {
            id++;
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
                    if(!(jobState == JobState.Stopped))
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
            this.StartTime = startTime;
            this.FinishTime = finishTime;
            this.MaxExecutionTime = maxExecutionTime;
        }

        //Start() is either going to start the job
        //Or it's going to resume the job if it was paused before
        internal void Start()
        {
            //2010, 1, 1 some default date time
            if (StartTime == new DateTime(2010, 1, 1))
            {
                tempTime = DateTime.Now;
            }
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        CheckStartTime();
                        jobState = JobState.Running;
                        //When job starts, thread is started, and we know what thread does, it causes mayhem :)
                        thread.Start();
                        break;
                    case JobState.RunningWithPauseRequest:
                        break;
                    case JobState.Running:
                        throw new InvalidOperationException("Job already started");
                    case JobState.Finished:
                        throw new InvalidOperationException("Job already finished");
                    case JobState.WaitingToResume:
                        jobState = JobState.Running;
                        resumeSemaphore.Release();
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
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                    case JobState.Stopped:
                        jobState = JobState.Finished;
                        /*if (numWaiters > 0)
                        {
                            finishedSemaphore.Release(numWaiters);
                        }*/
                        //onJobRelease(this)
                        //if(waited)
                        //{
                            //waited = false;
                            //LOGIC TO RELEASE THE TASK
                        //}
                        onJobFinished(this);
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
        /*internal void Wait()
        {
            if(!TaskScheduler.isOne)
            {
                lock (jobContextLock)
                {
                    switch (jobState)
                    {
                        case JobState.NotStarted:
                        case JobState.RunningWithPauseRequest:
                        case JobState.Running:
                            numWaiters++;
                            finishedSemaphore.Wait();
                            break;
                        case JobState.Finished:
                            return;
                        default:
                            throw new InvalidOperationException("Invalid job state");
                    }
                }
            }
            
            //finishedSemaphore.Wait();
        }*/

        internal void WaitAll()
        {
            lock(jobContextLock)
            {
                finishedSemaphore.Wait();
            }
        }

        internal void Wait(JobContext job)
        {
            lock(jobContextLock)
            {
                if(job.jobState == JobState.Running || job.jobState == JobState.RunningWithPauseRequest)
                {
                    job.waited = true;
                    finishedSemaphore.Wait();
                }
                else
                {
                    throw new InvalidOperationException("Can't wait for a job that is not running!");
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
                        onJobContinueRequested(this);
                        break;
                    case JobState.Stopped:
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
                        onJobPaused(this);                  
                        break;
                    case JobState.Running:
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
                        break;
                    case JobState.Stopped:
                        //jobState = JobState.Finished;
                        onJobStopped(this);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
        }

        //PROBABLY THERE IS A BETTER IMPLEMENTATION, AND THAT IS TO CHECK TIME SOMEWHERE INTERNALLY, THIS IS NOT PRECISE ENOUGH
        public bool CheckExecutionTime()
        {
            //If user didn't set max execution time, we don't really care
            if (MaxExecutionTime == 0)
            {
                return false;
            }
            //If execution time is longer that specified, it's time to stop
            TimeSpan ts = DateTime.Now - tempTime;
            double differenceInMilliseconds = ts.TotalMilliseconds;
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
            //Base case
            if (FinishTime.Year == 2010)
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
            if(StartTime.Year == 2010 || StartTime < DateTime.Now)
            {
                return;
            }
            Thread helpThread;
            helpThread = new(() =>
            {
                while(StartTime > DateTime.Now) { }
                onJobStarted(this);
            });
            helpThread.Start();
        }

        public JobState GetJobState()
        {
            return jobState;
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
    }
}
