using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    internal class JobContext : IJobContext
    {
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

        internal void Start()
        {
            lock(jobContextLock)
            {
                switch(jobState)
                {
                    case JobState.NotStarted:
                        jobState = JobState.Running;
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
