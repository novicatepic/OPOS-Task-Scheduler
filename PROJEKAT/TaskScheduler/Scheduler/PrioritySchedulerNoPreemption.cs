using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;

namespace TaskScheduler.Scheduler
{
    public class PrioritySchedulerNoPreemption : AbstractScheduler
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

        internal override void HandleResourceWanted(JobContext jobContext, Resource resource)
        {

            lock (schedulerLock)
            {
                bool someoneHoldingResource = false;
                //bool priorityChanged = false;
                //JobContext holder = null;
                foreach (var element in resourceMap)
                {
                    if (element.Value != null && element.Value.Contains(resource))
                    {
                        //someoneHoldingResource = true;
                        JobContext holder = element.Key;
                        
                        if(holder.Priority >= jobContext.Priority)
                        {
                            //REMOVE RESOURCE FROM CORRESPONDING JOB -> HAS LOWER PRIORITY
                            //Console.WriteLine("OVERTOOK");
                            element.Value.Remove(resource);
                        }
                        else
                        {
                            someoneHoldingResource = true;
                        }

                        //if(element.Va)
                        //jobContext.InversePriority(element.Key.Priority - 1);
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
                    jobContext.SetJobState(JobContext.JobState.Paused);
                    jobContext.shouldWaitForResource = true;
                }
            }
        }
    }
}
