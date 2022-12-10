using PROJEKAT;
using TaskScheduler;

TaskScheduler.TaskScheduler taskScheduler = new(false)
{
    MaxConcurrentTasks = 1
};

Job jobA = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 4 });

Job jobB = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 500
})
{ Priority = 5 });

Job jobc = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 5,
    SleepTime = 500
})


{ Priority = 7 });

Job jobX = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 10,
    SleepTime = 500
})
{ Priority = 1} );

Thread.Sleep(4000);
Console.WriteLine("Requesting pause on jobX");
jobX.RequestPause();

Thread.Sleep(1000);
Console.WriteLine("Requesting stoppage on jobX");
Thread.Sleep(500);
jobX.RequestStop();
Thread.Sleep(500);
Console.WriteLine("Requesting wait on jobB");
jobB.Wait();
jobX.RequestContinue();