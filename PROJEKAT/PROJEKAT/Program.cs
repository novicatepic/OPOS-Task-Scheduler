using PROJEKAT;
using TaskScheduler;

TaskScheduler.TaskScheduler taskScheduler = new(fifoflag: true)
{
    MaxConcurrentTasks = 2
};

Job jobA = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 10,
    SleepTime = 1000
})
{ Priority = 5, StartTime = new DateTime(2022, 12, 12, 7, 45, 30), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45), MaxExecutionTime = 5000}) ;

Job jobB = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 1000
})
{ Priority = 1 });

Job jobC = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 5,
    SleepTime = 500
})


{ Priority = 7 });

Job jobX = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 1} );

Thread.Sleep(1000);
jobA.Wait();
taskScheduler.ScheduleUnscheduledJob(jobB);
Thread.Sleep(1000);
jobA.RequestContinue();

Job manual = new Job(new JobSpecification(new DemoUserJob()
{
    Name = "Manually created job",
    NumIterations = 5,
    SleepTime = 750
}));
Thread.Sleep(1000);
//jobA.Wait(jobC);
manual.StartJobAsSeparateProcess();
//Thread.Sleep(1000);
manual.RequestPause();
Thread.Sleep(3000);
manual.RequestContinue();
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