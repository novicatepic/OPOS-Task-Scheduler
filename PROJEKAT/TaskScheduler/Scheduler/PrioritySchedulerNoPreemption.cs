﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerNoPreemption : NonPreemptiveScheduler
    {
        public PrioritySchedulerNoPreemption()
        {
            jobQueue = new Queue.PriorityQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            lock(schedulerLock)
            {
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

        /*internal override void HandleResourceWanted(JobContext jobContext, Resource resource)
        {

            lock (schedulerLock)
            {
                JobContext resourceHolder = null;
                bool someoneHoldingResource = false;
                foreach (var element in resourceMap)
                {
                    if (element.Value != null && element.Value.Contains(resource))
                    {
                        someoneHoldingResource = true;
                        //IF ELEMENT HOLDING THE RESOURCE HAS WEAKER PRIORITY
                        if(element.Key.Priority > jobContext.Priority)
                        {
                            //THEN REQUEST THE PRIORITY OF THE ELEMENT WITH BETTER PRIORITY (NO PREEMPTION)
                            element.Key.InversePriority(jobContext.Priority);
                        }
                        break;
                    }
                }
                if (someoneHoldingResource)
                {
                    for (int i = 0; i < resourceMap.Count; i++)
                    {
                        if (resourceMap.ElementAt(i).Value.Contains(resource))
                        {
                            if (!whoHoldsResources.ContainsKey(resourceMap.ElementAt(i).Key))
                            {
                                whoHoldsResources.Add(resourceMap.ElementAt(i).Key, new HashSet<JobContext>());
                            }
                            whoHoldsResources[resourceMap.ElementAt(i).Key].Add(jobContext);
                            break;
                        }
                    }

                }
                else
                {
                    if (!resourceMap.ContainsKey(jobContext))
                    {
                        resourceMap.Add(jobContext, new HashSet<Resource>());
                        HashSet<Resource> gotSet = new();
                        resourceMap[jobContext].Add(resource);
                    }
                    else
                    {
                        HashSet<Resource> gotSet = new();
                        resourceMap.TryGetValue(jobContext, out gotSet);
                        if (gotSet.Contains(resource))
                        {
                            throw new InvalidOperationException("Can't request a resource you are already holding!");
                        }
                        resourceMap[jobContext].Add(resource);
                    }
                }

                DeadlockDetectionGraph graph = MakeDetectionGraph();
                //Console.WriteLine("Matrix print!");
                //graph.PrintMatrix();

                bool cycleFound = graph.DFSForCycleCheck(jobContext.GetID());

                //PREVENT EVENTUAL CYCLE PROBLEMS WTIH cycleFound in if-condition
                if (someoneHoldingResource && cycleFound == false)
                {
                    //Adding next four lines of code so I can remember who is waiting on what
                    if (!jobWaitingOnResources.ContainsKey(jobContext))
                    {
                        jobWaitingOnResources.Add(jobContext, new HashSet<Resource>());
                    }
                    jobWaitingOnResources[jobContext].Add(resource);
                    //Pause the job and lock the semaphore
                    jobContext.SetJobState(JobContext.JobState.Paused);
                    jobContext.shouldWaitForResource = true;
                    //It's basically not in running jobs anymore
                    runningJobs.Remove(jobContext);
                    if (jobQueue.Count() > 0 && runningJobs.Count < MaxConcurrentTasks)
                    {
                        JobContext jb = jobQueue.Dequeue();
                        runningJobs.Add(jb);
                        jb.Start();
                    }
                }
                else if (cycleFound == true)
                {
                    Console.WriteLine("Resource not allowed, deadlock would be caused!");
                }
            }
        }*/
    }
}
