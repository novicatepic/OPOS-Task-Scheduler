using PROJEKAT;
using TaskScheduler;

TaskScheduler.TaskScheduler taskScheduler = new(true)
{
    MaxConcurrentTasks = 2
};

Job jobA = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 5,
    SleepTime = 2000
})
{ Priority = 4, MaxExecutionTime = 5000 });

Job jobB = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 2100
})
{ Priority = 5 });

Job jobX = taskScheduler.Schedule(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 10,
    SleepTime = 500
}));

Thread.Sleep(1000);
//Console.WriteLine("Requesting pause on jobX");
//jobX.RequestPause();
//Console.WriteLine("Requesting stoppage on jobA");
//jobA.RequestStop();
//Console.WriteLine("Requesting continue on jobX");
//jobX.RequestContinue();