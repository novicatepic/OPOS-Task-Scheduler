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
            this.sliceTime = sliceTime * 1000;
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
                    Thread.Sleep(15);
                    for (int i = 0; i < runningJobs.Count; i++)
                    {
                        if (runningJobs.Count > 0)
                        {
                            //Console.WriteLine("YES!");
                            TimeSpan ts = DateTime.Now - runningJobs.ElementAt(i).tempTime;
                            double ms = ts.TotalMilliseconds;
                            if (ms >= sliceTime && runningJobs.ElementAt(i).sliced == false)
                            {
                                Console.WriteLine("YES!");
                                //jb.shouldWaitForPriority = true;
                                runningJobs.ElementAt(i).RequestSliceStoppage();

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
                Console.WriteLine("PRIORaa: " + jobContext.Priority);
                runningJobs.Remove(jobContext);
                if (jobQueue.Count() > 0)
                {
                    JobContext dequeuedJobContext = jobQueue.Dequeue();
                    runningJobs.Add(dequeuedJobContext);
                    dequeuedJobContext.Start();
                }
                if (jobContext.sliced)
                {
                    //Console.WriteLine("SLICE");
                    //jobContext.shouldWaitForPriority = false;
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
