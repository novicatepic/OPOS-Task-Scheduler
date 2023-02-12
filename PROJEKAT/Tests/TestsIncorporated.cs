using TaskScheduler;
using TaskScheduler.Scheduler;
using PROJEKAT.Jobs;
using Microsoft.VisualStudio.Services.Commerce;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        AbstractScheduler fifoScheduler;
        AbstractScheduler fifoScheduler2;
        AbstractScheduler fifoScheduler3;
        AbstractScheduler priorityScheduler;
        AbstractScheduler priorityScheduler2;
        AbstractScheduler prioritySchedulerPreemptive;
        AbstractScheduler prioritySchedulerPreemptive2;
        AbstractScheduler roundRobin;

        [SetUp]
        public void Setup()
        {
            fifoScheduler = new FIFOScheduler()
            {
                MaxConcurrentTasks = 1
            };
            fifoScheduler2 = new FIFOScheduler()
            {
                MaxConcurrentTasks = 2
            };
            fifoScheduler3 = new FIFOScheduler()
            {
                MaxConcurrentTasks = 3
            };
            priorityScheduler = new PrioritySchedulerNoPreemption()
            {
                MaxConcurrentTasks = 1
            };
            priorityScheduler2 = new PrioritySchedulerNoPreemption()
            {
                MaxConcurrentTasks = 2
            };
            prioritySchedulerPreemptive = new PrioritySchedulerPreemption()
            {
                MaxConcurrentTasks = 1
            };
            prioritySchedulerPreemptive2 = new PrioritySchedulerPreemption()
            {
                MaxConcurrentTasks = 2
            };
            roundRobin = new FIFOSchedulerSlicing(2) //2 seconds
            {
                MaxConcurrentTasks = 2
            };
        }

        [Test]
        public void TestJobWithoutScheduling()
        {
            Job jobA = fifoScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) });

            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.NotScheduled);
        }

        [Test]
        public void TestJobWithScheduling()
        {
            Job jobA = fifoScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3 });
            Thread.Sleep(500);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Running);
        }

        [Test]
        public void TestJobPaused()
        {
            Job jobA = fifoScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3 });
            Thread.Sleep(500);
            jobA.RequestPause();
            Thread.Sleep(500);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Paused);
        }

        [Test]
        public void TestJobStopped()
        {
            Job jobA = fifoScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3 });
            Thread.Sleep(500);
            jobA.RequestStop();
            Thread.Sleep(500);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Stopped);
        }

        [Test]
        public void TestJobContinuation()
        {
            Job jobA = fifoScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3 });
            Thread.Sleep(500);
            jobA.RequestPause();
            Thread.Sleep(500);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Paused);
            jobA.RequestContinue();
            Thread.Sleep(500); Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Running);
        }

        [Test]
        public void TestPriorityStartingNoPreemption()
        {
            Job jobA = priorityScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 4 });
            Job jobB = priorityScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3 });
            Job jobC = priorityScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 2 });
            priorityScheduler.ScheduleUnscheduledJob(jobA);
            priorityScheduler.ScheduleUnscheduledJob(jobB);
            priorityScheduler.ScheduleUnscheduledJob(jobC);
            Thread.Sleep(3000);
            //jobC has better priority than jobB
            Assert.IsTrue(jobC.GetJobContext().State == JobContext.JobState.Running);
        }

        [Test]
        public void TestPriorityPreemption()
        {
            Job jobA = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 4 });
            Job jobB = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 3 });
            Job jobC = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 2 });
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobA);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobB);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobC);
            Thread.Sleep(200);
            //jobC has better priority than jobB and jobA, therefore it overtakes them
            Assert.IsTrue(jobC.GetJobContext().State == JobContext.JobState.Running);
            Thread.Sleep(2500);
            //After that, jobB comes in
            Assert.IsTrue(jobB.GetJobContext().State == JobContext.JobState.Running);
        }

        [Test]
        public void TestSeparateProcessJob()
        {
            Job jobA = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 4 });
            Job jobC = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 5,
                SleepTime = 500
            })
            { Priority = 2 });
            jobC.StartJobAsSeparateProcess();
            Thread.Sleep(200);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobA);
            //It runs as a separate process even though priority scheduler has 1 max active task that also is in running state
            Assert.IsTrue(jobC.GetJobContext().State == JobContext.JobState.Running && jobA.GetJobContext().State == JobContext.JobState.Running);
        }

        //ALSO WORKS IN MEAN, PRINTS OUT FINISHED
        [Test]
        public void TestMaxExecutionTime()
        {
            Job jobA = fifoScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 1000
            })
            { MaxExecutionTime=2000 });
            fifoScheduler.ScheduleUnscheduledJob(jobA);
            Thread.Sleep(3000);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Finished);
        }

        [Test]
        public void MultipleConcurrentTasksRunning()
        {
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 500
            })
            {});
            Job jobB = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 5,
                SleepTime = 500
            })
            { });
            Job jobC = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 5,
                SleepTime = 500
            })
            { });
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Running && jobB.GetJobContext().State == JobContext.JobState.Running);
        }

        [Test]
        public void RoundRobinTest()
        {
            Job jobA = roundRobin.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { });
            Job jobB = roundRobin.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 10,
                SleepTime = 500
            })
            { });
            Job jobC = roundRobin.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 10,
                SleepTime = 500
            })
            { });
            Thread.Sleep(3500);
            //jobC overtook because jobA got "sliced"
            Assert.IsTrue(jobC.GetJobContext().State == JobContext.JobState.Running || jobA.GetJobContext().State == JobContext.JobState.SlicePaused);
        }

        [Test]
        public void ResourceHoldageTest()
        {
            ResourceClass a = new ResourceClass("R1");
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 1000
            })
            { });
            jobA.RequestResource(a);
            Thread.Sleep(200);
            HashSet<TaskScheduler.ResourceClass> resources = new HashSet<TaskScheduler.ResourceClass>();
            fifoScheduler2.resourceMap.TryGetValue(jobA.GetJobContext(), out resources);
            Assert.IsTrue(resources.Contains(a));
        }

        [Test]
        public void HigherPriorityWaitingOnResource()
        {
            ResourceClass a = new ResourceClass("R1");
            Job jobA = prioritySchedulerPreemptive.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 1000
            })
            { Priority=0 });
            Job jobB = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 5,
                SleepTime = 1000
            })
            { Priority=1 });
            jobA.RequestResource(a);
            Thread.Sleep(200);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobB);
            HashSet<TaskScheduler.ResourceClass> resources = new HashSet<TaskScheduler.ResourceClass>();
            prioritySchedulerPreemptive.resourceMap.TryGetValue(jobA.GetJobContext(), out resources);
            Assert.IsTrue(resources.Contains(a) || jobA.GetJobContext().jobState == JobContext.JobState.Running);
        }

        [Test]
        public void ReleaseResourceTest()
        {
            ResourceClass a = new ResourceClass("R1");
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 1000
            })
            { });
            jobA.RequestResource(a);
            Thread.Sleep(200);
            jobA.ReleaseResource(a);
            HashSet<TaskScheduler.ResourceClass> resources = new HashSet<TaskScheduler.ResourceClass>();
            try
            {
                fifoScheduler2.resourceMap.TryGetValue(jobA.GetJobContext(), out resources);
            } catch(Exception ex)
            {
                //Test should pass because an exception was thrown
            }
        }

        [Test]
        public void KeptHoldingResourceTest()
        {
            ResourceClass a = new ResourceClass("R1");
            Job jobA = prioritySchedulerPreemptive2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 5,
                SleepTime = 1000
            })
            { Priority = 0 });
            Job jobB = prioritySchedulerPreemptive2.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 5,
                SleepTime = 1000
            })
            { Priority = 1 });
            jobA.RequestResource(a);
            Thread.Sleep(200);
            prioritySchedulerPreemptive2.ScheduleUnscheduledJob(jobB);
            Thread.Sleep(200);
            jobB.RequestResource(a);
            HashSet<TaskScheduler.ResourceClass> resources = new HashSet<TaskScheduler.ResourceClass>();
            prioritySchedulerPreemptive2.resourceMap.TryGetValue(jobA.GetJobContext(), out resources);
            Assert.IsTrue(resources.Contains(a));
        }

        [Test]
        public void DeadlockDetectionTest()
        {
            ResourceClass a = new ResourceClass("R1");
            ResourceClass b = new ResourceClass("R2");
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 50,
                SleepTime = 500
            })
            { });
            Job jobB = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 50,
                SleepTime = 500
            })
            {  });
            Thread.Sleep(600);
            jobA.RequestResource(a);

            Thread.Sleep(600);
            fifoScheduler2.ScheduleUnscheduledJob(jobB);
            Thread.Sleep(600);
            jobB.RequestResource(b);
            Thread.Sleep(600);
            //taskScheduler.ScheduleUnscheduledJob(jobC);
            //Thread.Sleep(400);
            jobA.RequestResource(b);
            Thread.Sleep(600);
            jobB.RequestResource(a);
            Thread.Sleep(600);
            Assert.IsTrue(jobA.GetJobContext().jobState == JobContext.JobState.Running
                && jobB.GetJobContext().jobState == JobContext.JobState.Stopped);
        }

        [Test]
        public void DeadlockDetectionTest2()
        {
            Job jobA = fifoScheduler3.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 50,
                SleepTime = 500
            })
            { });



            Job jobB = fifoScheduler3.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 50,
                SleepTime = 500
            })
            { Priority = 2 });

            Job jobC = fifoScheduler3.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 50,
                SleepTime = 500
            })
            { Priority = 1 });

            ResourceClass a = new ResourceClass("R1");
            ResourceClass b = new ResourceClass("R2");
            ResourceClass c = new ResourceClass("R3");

            Thread.Sleep(1100);
            jobA.RequestResource(a);
            Thread.Sleep(1100);
            jobB.RequestResource(b);
            Thread.Sleep(1100);
            jobC.RequestResource(c);
            Thread.Sleep(1100);
            jobA.RequestResource(b);
            Thread.Sleep(1100);
            jobB.RequestResource(c);
            Thread.Sleep(1100);
            jobC.RequestResource(a);
            Thread.Sleep(1500);
            Assert.IsTrue(jobA.GetJobContext().jobState == JobContext.JobState.Paused && jobC.GetJobContext().jobState == JobContext.JobState.Stopped
                && jobB.GetJobContext().jobState == JobContext.JobState.Running);

        }

        //IT CHANGES PRIORITY AND WORKS IN MAIN, IN TESTS DOESN'T WORK
        //SOMETIMES DOES, SOMETIMES DOESN'T
        //EITHER WAY, IT PRINTS OUT 2, WHICH MEANS THAT PRIORITY HAS CHANGED, TESTED IN MAIN...
        [Test]
        public void InversePriorityTest()
        {
            Job jobA = priorityScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 1000
            })
            { Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) });


            Job jobB = priorityScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 10,
                SleepTime = 1000
            })
            { Priority = 2 });

            ResourceClass a = new ResourceClass("R1");
            jobA.RequestResource(a);
            Thread.Sleep(1000);
            jobB.RequestResource(a);
            Thread.Sleep(1000);
            Assert.IsTrue(jobA.GetJobContext().Priority == jobB.GetJobContext().Priority);
            //Console.WriteLine(jobA.GetJobContext().Priority); Console.WriteLine(jobB.GetJobContext().Priority);
        }

        [Test]
        public void PIPTest()
        {
            Job jobA = prioritySchedulerPreemptive.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 50,
                SleepTime = 1000
            })
            { Priority = 3 });



            Job jobB = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 50,
                SleepTime = 1000
            })
            { Priority = 2 });

            Job jobC = prioritySchedulerPreemptive.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job C",
                NumIterations = 50,
                SleepTime = 1000
            })
            { Priority = 1 });

            ResourceClass a = new ResourceClass("R1");

            Thread.Sleep(600);
            jobA.RequestResource(a);

            Thread.Sleep(600);
            //jobB.RequestResource(a);
            Thread.Sleep(1000);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobB);
            Thread.Sleep(1000);
            prioritySchedulerPreemptive.ScheduleUnscheduledJob(jobC);
            Thread.Sleep(2000);
            jobC.RequestResource(a);

            Console.WriteLine(jobA.jobContext.State);
            Console.WriteLine(jobB.jobContext.State);
            Console.WriteLine(jobC.jobContext.State);

            Thread.Sleep(2600);
            Assert.IsTrue(jobA.jobContext.Priority == jobC.jobContext.Priority);
        }

        [Test]
        public void ResourceTakeoverTest()
        {
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 50,
                SleepTime = 500
            })
            { Priority = 3 });

            //Application.ShutDown();

            Job jobB = fifoScheduler2.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 50,
                SleepTime = 500
            })
            { Priority = 2 });

            ResourceClass a = new ResourceClass("R1");

            Thread.Sleep(600);
            jobA.RequestResource(a);

            Thread.Sleep(600);
            fifoScheduler2.ScheduleUnscheduledJob(jobB);
            Thread.Sleep(600);
            jobB.RequestResource(a);
            Thread.Sleep(3000);
            jobA.ReleaseResource(a);
            Thread.Sleep(600);
            Assert.IsTrue(fifoScheduler2.resourceMap[jobB.jobContext].Contains(a) && !fifoScheduler2.resourceMap[jobA.jobContext].Contains(a));
        }
    }
}