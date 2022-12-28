using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;

namespace TaskScheduler.Scheduler
{
    //Basically round robin algorithm, not 100% precise, will rename it
    public class FIFOSchedulerSlicing : AbstractScheduler, ISlicer
    {
        private int sliceTime = 0;

        public FIFOSchedulerSlicing(int sliceTime)
        {
            jobQueue = new FIFOQueue();
            this.sliceTime = sliceTime * 1000; //Convert to milliseconds
        }

        internal override void ScheduleJob(JobContext jobContext)
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
                    CheckSliceTime();
                }
            }
            if (one)
            {
                //CheckSliceTime();
                one = false;
            }

        }
        bool one = true;

        public void CheckSliceTime()
        {
            Thread helpThread;
            helpThread = new(() =>
            {
                while (runningJobs.Count > 0 || jobQueue.Count() > 0)
                {
                    //Optimising so I don't get fault
                    //Was 15
                    Thread.Sleep(200);
                    for (int i = 0; i < runningJobs.Count; i++)
                    {
                        if (runningJobs.Count > 0)
                        {
                            //Console.WriteLine("YES!");
                            TimeSpan ts = DateTime.Now - runningJobs.ElementAt(i).tempTime;
                            double ms = ts.TotalMilliseconds;
                            
                            if (ms >= sliceTime && runningJobs.ElementAt(i).sliced == false && !(runningJobs.ElementAt(i).jobState == JobContext.JobState.Stopped)
                                    && CheckIfEveryJobIsPaused())
                            {
                                Console.WriteLine("YES!");
                                runningJobs.ElementAt(i).RequestSliceStoppage();
                            }
                        }

                    }
                }
            });
            helpThread.Start();
        }

        private bool CheckIfEveryJobIsPaused()
        {
            FIFOQueue fIFOQueue = (FIFOQueue )jobQueue;
            lock(schedulerLock)
            {
                bool notPaused = false;
                foreach (JobContext element in fIFOQueue.ReturnQueue())
                {
                    //If sliced don't do anything
                    if (element.jobState == JobContext.JobState.Paused)
                    {
                        //return false;
                    }
                    //If not sliced switch the job so there is a not sliced job
                    else
                    {
                        notPaused = true;
                    }
                }
                return notPaused;
            }
            
        }

        internal override void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                FIFOQueue fIFOQueue = (FIFOQueue)jobQueue;
                //Console.WriteLine("PRIORaa: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0 && CheckIfEveryJobIsPaused())
                {
                    foreach(JobContext element in fIFOQueue.ReturnQueue())
                    {
                        if(element.jobState != JobContext.JobState.Paused)
                        {
                            fIFOQueue.ReorderQueue(element);
                            runningJobs.Add(element);
                            element.Start();
                            break;
                        }
                    }
                    /*JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();*/
                }
                if (jobContext.sliced)
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
            }
        }

        internal override void HandleJobContinueRequested(JobContext jobContext)
        {
            if (jobContext.GetJobState() == JobContext.JobState.Stopped)
            {
                throw new InvalidOperationException("Can't continue a stopped job!");
            }

            lock (schedulerLock)
            {
                if(jobContext.sliced)
                {
                    jobContext.sliced = false;
                }
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

        internal override void HandleJobFinished(JobContext jobContext)
        {

            lock (schedulerLock)
            {
                jobContext.SetJobState(JobContext.JobState.Finished);
                FIFOQueue fIFOQueue = (FIFOQueue)jobQueue;
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    foreach (JobContext element in fIFOQueue.ReturnQueue())
                    {
                        if (element.jobState != JobContext.JobState.Paused)
                        {
                            fIFOQueue.ReorderQueue(element);
                            runningJobs.Add(element);
                            element.Start();
                            break;
                        }
                    }
                    //JobContext dequeuedJobContext = jobQueue.Dequeue();
                    //runningJobs.Add(dequeuedJobContext);
                    //dequeuedJobContext.Start();
                }

            }
        }
    }
}
