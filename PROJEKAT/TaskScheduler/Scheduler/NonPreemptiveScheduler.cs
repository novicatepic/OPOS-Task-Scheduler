using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;

namespace TaskScheduler.Scheduler
{
    public class NonPreemptiveScheduler : AbstractScheduler
    {
        protected Dictionary<ResourceClass, int> rememberPast = new();

        internal override void ScheduleJob(JobContext jobContext)
        {
            //throw new NotImplementedException();
        }

        internal override void HandleResourceWanted(JobContext jobContext, ResourceClass resource)
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
                        //THEN INVERSE PRIORITY
                        if (element.Key.Priority > jobContext.Priority/* && element.Key.oldPriority == -1*/)
                        {
                            if (!rememberPast.ContainsKey(resource))
                            {                               
                                rememberPast.Add(resource, jobContext.Priority);
                                element.Key.InversePriority(jobContext.Priority);
                                Console.WriteLine("INVERSED!!!");
                                Console.WriteLine(element.Key.Priority);
                                //CheckPreemption(element.Key);
                            }

                            else if (rememberPast.ContainsKey(resource) && rememberPast[resource] > jobContext.Priority)
                            {
                                Console.WriteLine("ELSE");
                                rememberPast[resource] = jobContext.Priority;
                                element.Key.InversePriority(jobContext.Priority);
                                //CheckPreemption(element.Key);
                            }
                            //Console.WriteLine("PRIORITY INVERSED!");

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
                        resourceMap.Add(jobContext, new HashSet<ResourceClass>());
                        HashSet<ResourceClass> gotSet = new();
                        resourceMap[jobContext].Add(resource);
                    }
                    else
                    {
                        HashSet<ResourceClass> gotSet = new();
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
                        jobWaitingOnResources.Add(jobContext, new HashSet<ResourceClass>());
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
        }

        internal override void HandleResourceReleased(JobContext jobContext, ResourceClass resource)
        {
            lock (schedulerLock)
            {
                if (resourceMap.ContainsKey(jobContext))
                {
                    if (resourceMap[jobContext].Contains(resource))
                    {
                        resourceMap[jobContext].Remove(resource);
                        if (jobContext.oldPriority != -1)
                        {
                            int prior = rememberPast[resource];
                            jobContext.ReversePriority();
                            Console.WriteLine("GOT BACK PRIORITY " + jobContext.Priority);
                        }
                        foreach (var job in jobWaitingOnResources)
                        {
                            if (job.Value.Contains(resource))
                            {
                                //Job doesn't wait on that resource anymore and that resource can continue to work :)
                                //But only if he doesn't wait for anything else
                                job.Value.Remove(resource);
                                if (job.Value.Count == 0 && runningJobs.Count < MaxConcurrentTasks)
                                {
                                    runningJobs.Add(job.Key);
                                    job.Key.Start();
                                }
                                else if (job.Value.Count == 0)
                                {
                                    //jobQueue.Enqueue(job.Key, job.Key.Priority);
                                    //CheckPreemption(job.Key);
                                }
                                whoHoldsResources[jobContext].Remove(job.Key);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't release a resource which is not held by the job!");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Can't release a resource which is not held by the job!");
                }
            }
        }

    }
}
