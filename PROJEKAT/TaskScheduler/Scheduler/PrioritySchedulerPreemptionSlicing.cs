﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerPreemptionSlicing : AbstractScheduler, IPreemption, ISlicer
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

        public void CheckPreemption(JobContext jobContext)
        {
            jobQueue.Enqueue(jobContext, jobContext.Priority);
            int maxPriority = -1;
            int index = -1;
            for (int i = 0; i < runningJobs.Count; i++)
            {
                if (runningJobs.ElementAt(i).Priority > jobContext.Priority && runningJobs.ElementAt(i).Priority > maxPriority)
                {
                    maxPriority = runningJobs.ElementAt(i).Priority;
                    index = i;
                }
            }
            if (index != -1)
            {
                runningJobs.ElementAt(index).RequestPriorityStoppage();
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
                    Thread.Sleep(15);
                    for (int i = 0; i < runningJobs.Count; i++)
                    {
                        if (runningJobs.Count > 0)
                        {
                            //Console.WriteLine("YES!");
                            TimeSpan ts = DateTime.Now - runningJobs.ElementAt(i).tempTime;
                            double ms = ts.TotalMilliseconds;
                            if (ms >= runningJobs.ElementAt(i).sliceTime && runningJobs.ElementAt(i).sliced == false)
                            {
                                Thread.Sleep(50);
                                //Console.WriteLine("YES!");
                                //jb.shouldWaitForPriority = true;
                                if (!(runningJobs.Count == 1 && jobQueue.Count() == 0))
                                {
                                    if (runningJobs.Count != 0)
                                    {
                                        runningJobs.ElementAt(i).RequestSliceStoppage();
                                    }
                                }
                            }
                        }

                    }
                }
            });
            helpThread.Start();
        }


        internal override void HandleJobPaused(JobContext jobContext)
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
                if (jobContext.sliced)
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
                if (jobContext.shouldWaitForPriority)
                {
                    jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
            }
        }

        /*internal override void HandleJobFinished(JobContext jobContext)
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
        }*/

    }
}
