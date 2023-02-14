using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerNoPreemptionSlicing : NonPreemptiveScheduler, ISlicer
    {

        public PrioritySchedulerNoPreemptionSlicing()
        {
            jobQueue = new FIFOQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                //Stupid logic. set slice time based on this
                jobContext.SetSliceTime(5 - jobContext.Priority);
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
        }

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
                            //If a job has been running more than it should have
                            if (ms >= runningJobs.ElementAt(i).sliceTime && runningJobs.ElementAt(i).sliced == false && !(runningJobs.ElementAt(i).jobState == JobContext.JobState.Stopped)
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

        //HELP FUNCTION FOR GUI
        //This is a bad function, it's basically check if every job is not paused
        private bool CheckIfEveryJobIsPaused()
        {
            FIFOQueue fIFOQueue = (FIFOQueue)jobQueue;
            lock (schedulerLock)
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

        //Reorder queue necessary, because some jobs are paused and there is a problem
        internal override void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                FIFOQueue fIFOQueue = (FIFOQueue)jobQueue;
                //Console.WriteLine("PRIORaa: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0 && CheckIfEveryJobIsPaused())
                {
                    foreach (JobContext element in fIFOQueue.ReturnQueue())
                    {
                        //If there is an element not paused, no problems
                        if (element.jobState != JobContext.JobState.Paused)
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
                //Not sliced anymore
                if (jobContext.sliced)
                {
                    jobContext.sliced = false;
                }
                //So either start it 
                if (runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                //Or make it wait
                else
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
            }
        }

        internal override void HandleJobFinished(JobContext jobContext)
        {
            //Need to find element that is not slice paused and start it (if it exists)
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
                }

            }
        }


    }
}
