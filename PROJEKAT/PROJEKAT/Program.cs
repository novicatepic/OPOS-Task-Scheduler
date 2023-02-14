using Microsoft.EntityFrameworkCore.ChangeTracking;
using PROJEKAT;
using PROJEKAT.Jobs;
using System.Drawing;
using System.Drawing.Imaging;
using TaskScheduler;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;



AbstractScheduler blaaa = new FIFOSchedulerSlicing(2)
{
    MaxConcurrentTasks = 2
};

AbstractScheduler taskScheduler = new PrioritySchedulerPreemption()
{
    MaxConcurrentTasks = 1
};


Job jobA = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoPIP()
{
    Name = "Job A",
    NumIterations = 50,
    SleepTime = 500
})
{ Priority = 3 });

//Application.ShutDown();

Job jobB = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 50,
    SleepTime = 500
})
{ Priority = 2 });

Job jobC = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoPIP()
{
    Name = "Job C",
    NumIterations = 50,
    SleepTime = 500
})
{ Priority = 1 });

Thread.Sleep(100);
taskScheduler.ScheduleUnscheduledJob(jobA);

Thread.Sleep(400);
taskScheduler.ScheduleUnscheduledJob(jobB);

Thread.Sleep(400);
taskScheduler.ScheduleUnscheduledJob(jobC);
Console.WriteLine("JA= " + jobA.jobContext.State);
Console.WriteLine("JB= " + jobB.jobContext.State);
Console.WriteLine("JC= " + jobC.jobContext.State);




