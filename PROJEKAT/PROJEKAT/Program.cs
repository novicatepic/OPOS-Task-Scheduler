using PROJEKAT;
using TaskScheduler;

TaskScheduler.TaskScheduler taskScheduler = new(fifoflag: true)
{
    MaxConcurrentTasks = 2
};

Job jobA = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 1, StartTime = new DateTime(2022, 12, 11, 6, 57, 52)});

Job jobB = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 5 });

Job jobC = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 5,
    SleepTime = 500
})


{ Priority = 7 });

Job jobX = taskScheduler.ScheduleWithStart(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 1} );

Thread.Sleep(1000);
jobA.Wait(jobB);
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

Console.WriteLine("Requesting pause on jobX");
//jobX.RequestPause();

Thread.Sleep(1000);
Console.WriteLine("Requesting stoppage on jobX");
Thread.Sleep(500);
//jobX.RequestStop();
Thread.Sleep(500);
Console.WriteLine("Requesting wait on jobB");
Thread.Sleep(10000);
//taskScheduler.ScheduleUnstartedJob(jobX);
//jobB.Wait();
//jobX.RequestContinue();