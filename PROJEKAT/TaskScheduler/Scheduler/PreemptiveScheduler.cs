using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Graph;

namespace TaskScheduler.Scheduler
{
    public class PreemptiveScheduler : AbstractScheduler, IPreemption
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
                        if (element.Key.Priority > jobContext.Priority/* && element.Key.oldPriority == -1*/)
                        {
                            if (!rememberPast.ContainsKey(resource))
                            {
                                //Console.WriteLine("INVERSED PRIORITY!");
                                rememberPast.Add(resource, jobContext.Priority);
                                element.Key.InversePriority(jobContext.Priority);
                                CheckPreemption(element.Key);
                            }

                            else if (rememberPast.ContainsKey(resource) && rememberPast[resource] > jobContext.Priority)
                            {
                                //Console.WriteLine("ELSE");
                                rememberPast[resource] = jobContext.Priority;
                                element.Key.InversePriority(jobContext.Priority);
                                CheckPreemption(element.Key);
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
                    deadlockJobs.Add(jobContext);
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
                    Console.WriteLine("REQUEST STOPPAGE");
                    HashSet<ResourceClass> resources = resourceMap[jobContext];
                    foreach (var res in resources)
                    {
                        HandleResourceReleased(jobContext, res);
                    }
                    jobContext.RequestStop();
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
                                    if (deadlockJobs.Contains(job.Key))
                                    {
                                        deadlockJobs.Remove(job.Key);
                                    }

                                    if (!resourceMap.ContainsKey(job.Key))
                                    {
                                        resourceMap.Add(job.Key, new HashSet<ResourceClass>());
                                        resourceMap[job.Key].Add(resource);
                                        Console.WriteLine("ADDED");
                                    }
                                    else
                                    {
                                        Console.WriteLine("ADDED");
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    if (jobWaitingOnResources.ContainsKey(jobContext))
                                    {
                                        jobWaitingOnResources[jobContext].Remove(resource);
                                    }

                                    runningJobs.Add(job.Key);
                                    job.Key.Start();
                                }
                                else if (job.Value.Count == 0)
                                {
                                    if (!resourceMap.ContainsKey(job.Key))
                                    {
                                        resourceMap.Add(job.Key, new HashSet<ResourceClass>());
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    else
                                    {
                                        resourceMap[job.Key].Add(resource);
                                    }
                                    if (jobWaitingOnResources.ContainsKey(jobContext))
                                    {
                                        jobWaitingOnResources[jobContext].Remove(resource);
                                    }
                                    CheckPreemption(job.Key);
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

        //So basically, if I can "throw out" a job with worse priority, I'll have to request priority stoppage, and it will work
        public void CheckPreemption(JobContext jobContext)
        {
            jobQueue.Enqueue(jobContext, jobContext.Priority);
            int maxPriority = -1;
            int index = -1;
            for (int i = 0; i < runningJobs.Count; i++)
            {
                //WHENEVER A JOB IS ADDED, CHECK PRIORITY
                //POTENTIAL PROBLEM -> BE CAREFUL (WHEN TWO JOBS ARE ADDED NEXT TO EACH OTHER SAME JOB IS GOING TO BE PREEMPTED
                //THEREFORE, PROBABLY SHOULD HAVE ANOTHER CONDITION IN IF STATEMENT
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

        internal override void HandleJobContinueRequested(JobContext jobContext)
        {
            if (jobContext.GetJobState() == JobContext.JobState.Stopped)
            {
                throw new InvalidOperationException("Can't continue a stopped job!");
            }

            lock (schedulerLock)
            {
                if (runningJobs.Count < MaxConcurrentTasks)
                {
                    runningJobs.Add(jobContext);
                    jobContext.Start();
                }
                else
                {
                    //CHECK PREEMPTION WHEN RELEASED FROM PAUSE
                    CheckPreemption(jobContext);
                    //jobQueue.Enqueue(jobContext, jobContext.Priority);
                }
            }
        }
    }
}
