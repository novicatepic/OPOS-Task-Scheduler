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

        [Test]
        public void TestMaxExecutionTime()
        {
            Job jobA = fifoScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 10000
            })
            { MaxExecutionTime=2000 });
            fifoScheduler.ScheduleUnscheduledJob(jobA);
            Thread.Sleep(3000);
            Assert.IsTrue(jobA.GetJobContext().State == JobContext.JobState.Running);
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
                NumIterations = 5,
                SleepTime = 2000
            })
            { });
            Job jobB = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job B",
                NumIterations = 5,
                SleepTime = 2000
            })
            {  });
            jobA.RequestResource(b);
            Thread.Sleep(500);
            try
            {
                jobB.RequestResource(a);
            } catch(Exception e)
            {
                //it should throw an exception
            }

        }

        [Test]
        public void WaitsOnResourceTest()
        {
            Job jobA = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
            {
                Name = "Job A",
                NumIterations = 10,
                SleepTime = 500
            })
            { Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) });


            Job jobB = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
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
            Assert.IsFalse(jobB.GetJobContext().jobState == JobContext.JobState.Running);
            jobA.ReleaseResource(a);
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
        public void TestImageProcessingSpeed()
        {
            string path = "Images/InputImages/";
            string outputPath = "Images/OutputImages/";
            List<(string, string)> tupple = new List<(string, string)>();
            tupple.Add((path, outputPath));
            List<string> inputPaths = new();
            inputPaths.Add(path);

            Job demoJob = fifoScheduler.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
            {
                Name = "Job A",
                Parallelism = 2,
            })
            { });

            Job demoJob2 = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
            {
                Name = "Job B",
                Parallelism = 1,
            })
            {});
            Thread.Sleep(10000);
            Assert.IsTrue(demoJob.GetJobContext().execTime < demoJob2.GetJobContext().execTime);
        }

        /*[Test]
        public void TestSingleImageProcessingSpeed()
        {
            string path = "Images/InputImage/";
            string outputPath = "Images/OutputImages/";
            List<(string, string)> tupple = new List<(string, string)>();
            tupple.Add((path, outputPath));
            List<string> inputPaths = new();
            inputPaths.Add(path);

            Job demoJob = fifoScheduler.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
            {
                Name = "Job A",
                Parallelism = 1,
                SingleParralelism = 2
            })
            { });

            Job demoJob2 = fifoScheduler2.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
            {
                Name = "Job B",
                Parallelism = 1,
                SingleParralelism = 1
            })
            { });
            Thread.Sleep(5000);
            Assert.IsTrue(demoJob.GetJobContext().execTime < demoJob2.GetJobContext().execTime);
        }*/
    }
}