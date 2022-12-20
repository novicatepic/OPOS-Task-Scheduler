using PROJEKAT;
using TaskScheduler;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;

AbstractScheduler blaaa = new FIFOSchedulerSlicing(2)
{
    MaxConcurrentTasks = 2
};

AbstractScheduler taskScheduler = new PrioritySchedulerNoPreemption()
{
    MaxConcurrentTasks = 2
    //jobQueue = new PriorityQueue()
};

//PriorityQueue pq = taskScheduler.jobQueue as  PriorityQueue;
//pq.SetWithPreemption(true);

Job jobA = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 100,
    SleepTime = 1000
})
{ Priority = 2, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45)});

Job jobB = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 1 });

Job jobC = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 3 });

Job jobX = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 4} );

Resource a = new Resource("R1");
Resource b = new Resource("R2");
Resource c = new Resource("R3");
//Resource d = new Resource("R2");
Thread.Sleep(200);
jobA.RequestResource(a);
//jobA.RequestPause();
Thread.Sleep(300);
//jobA.RequestContinue();
jobB.RequestResource(a);
//Thread.Sleep(200);
//jobC.RequestResource(c);
//Thread.Sleep(200);
//jobA.RequestResource(b);
//Thread.Sleep(1000);
//jobB.RequestResource(c);
//Thread.Sleep(1000);
//jobC.RequestResource(a);
//jobB.RequestResource(a);
//jobB.RequestResource(a);
//jobA.RequestResource(b);
//Thread.Sleep(2000);
//jobC.RequestResource(a);

/*Job jobX2 = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X1",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 0 });*/

//Thread.Sleep(1000);

//jobC.RequestPause();
//jobB.RequestPause();
//jobA.Wait();
//taskScheduler.ScheduleUnscheduledJob(jobB);
//Thread.Sleep(5000);
//jobC.RequestContinue();
//jobB.RequestContinue();
//jobA.RequestContinue();

/*Job manual = new Job(new JobSpecification(new DemoUserJob()
{
    Name = "Manually created job",
    NumIterations = 5,
    SleepTime = 750
}));*/
//Thread.Sleep(1000);
//jobA.Wait();
//jobA.Wait(jobC);
//manual.StartJobAsSeparateProcess();
//Thread.Sleep(1000);
//manual.RequestPause();
//Thread.Sleep(6000);
//manual.RequestContinue();
//manual.Wait(jobA);
//manual.Wait();


//jobA.Wait(jobB);
//jobB.Wait(jobC);

/*Job jobB1 = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job B1",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 5 });

Job jobC1 = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job C1",
    NumIterations = 5,
    SleepTime = 500
}));
Thread.Sleep(6500);
jobB1.Wait(jobC1);*/

//Console.WriteLine("Requesting pause on jobX");
//jobX.RequestPause();

//Thread.Sleep(1000);
//Console.WriteLine("Requesting stoppage on jobX");
//Thread.Sleep(500);
//jobX.RequestStop();
//Thread.Sleep(500);
//Console.WriteLine("Requesting wait on jobB");
//Thread.Sleep(10000);
//taskScheduler.ScheduleUnstartedJob(jobX);
//jobB.Wait();
//jobX.RequestContinue();