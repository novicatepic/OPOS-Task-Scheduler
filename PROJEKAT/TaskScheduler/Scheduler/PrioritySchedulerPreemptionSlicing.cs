using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerPreemptionSlicing : PreemptiveScheduler, ISlicer
    {

        public PrioritySchedulerPreemptionSlicing()
        {
            jobQueue = new PriorityQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                jobContext.SetSliceTime(5 - jobContext.Priority);
                if (runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                else
                {
                    //jobQueue.Enqueue(jobContext, jobContext.Priority);
                    CheckPreemption(jobContext);
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


        private bool CheckIfEveryJobIsPaused()
        {
            lock (schedulerLock)
            {
                PriorityQueue<JobContext, int> temp = new PriorityQueue<JobContext, int>();
                bool notPaused = false;
                while (jobQueue.Count() > 0)
                {
                    JobContext element = jobQueue.Dequeue();
                    temp.Enqueue(element, element.Priority);
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
                while (temp.Count > 0)
                {
                    var element = temp.Dequeue();
                    jobQueue.Enqueue(element, element.Priority);
                }
                return notPaused;
            }

        }

        //Reorder queue necessary, because some jobs are paused and there is a problem
        internal override void HandleJobPaused(JobContext jobContext)
        {
            lock (schedulerLock)
            {
                PriorityQueue<JobContext, int> temp = new PriorityQueue<JobContext, int>();
                //Console.WriteLine("PRIORaa: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                Boolean found = false;
                while (jobQueue.Count() > 0)
                {
                    JobContext element = jobQueue.Dequeue();
                    if (element.jobState != JobContext.JobState.Paused && !found)
                    {
                        runningJobs.Add(element);
                        element.Start();
                        found = true;
                    }
                    else
                    {
                        temp.Enqueue(element, element.Priority);
                    }
                }
                while (temp.Count > 0)
                {
                    var element = temp.Dequeue();
                    jobQueue.Enqueue(element, element.Priority);
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
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    PriorityQueue<JobContext, int> temp = new PriorityQueue<JobContext, int>();
                    //Console.WriteLine("PRIORaa: " + jobContext.Priority);
                    bool found = false;
                    while (jobQueue.Count() > 0)
                    {
                        JobContext element = jobQueue.Dequeue();
                        if (element.jobState != JobContext.JobState.Paused && !found)
                        {
                            runningJobs.Add(element);
                            element.Start();
                            found = true;
                        }
                        else
                        {
                            temp.Enqueue(element, element.Priority);
                        }
                    }
                    while (temp.Count > 0)
                    {
                        var element = temp.Dequeue();
                        jobQueue.Enqueue(element, element.Priority);
                    }
                }

            }
        }

    }
}
