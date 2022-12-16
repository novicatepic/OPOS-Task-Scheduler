using PROJEKAT;
using TaskScheduler;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;

AbstractScheduler blaaa = new FIFOSchedulerSlicing(2)
{
    MaxConcurrentTasks = 1
};

AbstractScheduler taskScheduler = new PrioritySchedulerPreemption()
{
    MaxConcurrentTasks = 1,
    //jobQueue = new PriorityQueue()
};

//PriorityQueue pq = taskScheduler.jobQueue as  PriorityQueue;
//pq.SetWithPreemption(true);

Job jobA = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 4, StartTime = new DateTime(2022, 12, 12, 7, 45, 30), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45)}) ;

Job jobB = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 2 });

Job jobC = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 1 });

Job jobX = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 3} );

/*Job jobX2 = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X1",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 0 });*/

Thread.Sleep(1000);

//jobC.RequestPause();
//jobB.RequestPause();
//jobA.Wait();
//taskScheduler.ScheduleUnscheduledJob(jobB);
Thread.Sleep(5000);
//jobC.RequestContinue();
//jobB.RequestContinue();
//jobA.RequestContinue();

/*Job manual = new Job(new JobSpecification(new DemoUserJob()
{
    Name = "Manually created job",
    NumIterations = 5,
    SleepTime = 750
}));*/
Thread.Sleep(1000);
//jobA.Wait();
//jobA.Wait(jobC);
//manual.StartJobAsSeparateProcess();
//Thread.Sleep(1000);
//manual.RequestPause();
Thread.Sleep(3000);
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