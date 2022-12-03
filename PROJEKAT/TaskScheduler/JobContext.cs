using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    internal class JobContext : IJobContext
    {
        //Concerning job, each job has it's own state
        private enum JobState
        {
            NotStarted, 
            Running, 
            RunningWithPauseRequest,
            WaitingToResume,
            Paused,
            Finished 
        }

        private JobState jobState = JobState.NotStarted;
        private readonly Thread thread;
        private readonly object jobContextLock = new();
        private readonly Action<JobContext> onJobFinished;
        private readonly Action<JobContext> onJobPaused;
        private readonly Action<JobContext> onJobContinueRequested;
        internal int Priority { get; init; }
        private readonly SemaphoreSlim finishedSemaphore = new(0);
        private readonly SemaphoreSlim resumeSemaphore = new(0);
        private int numWaiters = 0;

        public JobContext(IUserJob userJob, int priority, Action<JobContext> onJobFinished, Action<JobContext> onJobPaused, Action<JobContext> onJobContinueRequested)
        {
            thread = new(() =>
            {
                //Calls Run method from and Finishes, but it's not started yet, only declaring what thread will do
                try
                {
                    userJob.Run(this);
                }
                finally
                {
                    Finish();
                }
            });

            Priority = priority;
            this.onJobFinished = onJobFinished;
            this.onJobPaused = onJobPaused;
            this.onJobContinueRequested = onJobContinueRequested;
        }

        //Start() is either going to start the job
        //Or it's going to resume the job if it was paused before
        internal void Start()
        {
            lock(jobContextLock)
            {
                switch(jobState)
                {
                    case JobState.NotStarted:
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
                        jobState = JobState.Finished;
                        if(numWaiters > 0)
                        {
                            finishedSemaphore.Release(numWaiters);
                        }
                        onJobFinished(this); 
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Job already finished.");
                    default:
                        throw new InvalidOperationException("Invalid job state");

                }
            }
        }

        //Thread doesn't run anymore but it waits (semaphore) and increases numWaiters so they can be released
        internal void Wait()
        {
            lock (jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                    case JobState.RunningWithPauseRequest:
                    case JobState.Running:
                        numWaiters++;
                        break; 
                    case JobState.Finished:
                        return;
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }
            finishedSemaphore.Wait();
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
            lock(jobContextLock)
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
                    case JobState.Finished:
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
            lock(jobContextLock)
            {
                switch (jobState)
                {
                    case JobState.NotStarted:
                        throw new InvalidOperationException("Invalid job state.");
                    case JobState.RunningWithPauseRequest:
                        jobState = JobState.Paused;
                        onJobPaused(this);
                        shouldPause = true;
                        break;
                    case JobState.Running:
                        break;
                    case JobState.Finished:
                        throw new InvalidOperationException("Invalid job state.");
                    default:
                        throw new InvalidOperationException("Invalid job state");
                }
            }

            if(shouldPause)
            {
                resumeSemaphore.Wait();
            }
        }

    }
}
