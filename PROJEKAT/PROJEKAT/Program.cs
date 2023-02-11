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

AbstractScheduler taskScheduler = new FIFOScheduler()
{
    MaxConcurrentTasks = 1
};


Job jobA = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 50,
    SleepTime = 1000
})
{ Priority = 3 });



Job jobB = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 50,
    SleepTime = 1000
})
{ Priority = 2 });

Job jobC = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 50,
    SleepTime = 1000
})
{ Priority = 1 });

/*Job jobX = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations =10,
    SleepTime = 500
})
{ Priority = 0} );*/

ResourceClass a = new ResourceClass("R1");
ResourceClass b = new ResourceClass("R2");
ResourceClass c = new ResourceClass("R3");

/*Thread.Sleep(600);
jobA.RequestResource(a);

Thread.Sleep(600);
//jobB.RequestResource(a);
Thread.Sleep(1000);
taskScheduler.ScheduleUnscheduledJob(jobB);
Thread.Sleep(1000);
taskScheduler.ScheduleUnscheduledJob(jobC);
Thread.Sleep(2000);
jobC.RequestResource(a);

Console.WriteLine(jobA.jobContext.State);
Console.WriteLine(jobB.jobContext.State);
Console.WriteLine(jobC.jobContext.State);

Thread.Sleep(2000);
Console.WriteLine(jobA.jobContext.Priority);*/

//Thread.Sleep(3000);
//Console.WriteLine("STATE="+jobA.GetJobContext().State);

//Thread.Sleep(1000);
//Console.WriteLine("PAUSE REQUESTED!");
//jobB.RequestPause();
//taskScheduler.ScheduleUnscheduledJob(jobC);
//Console.WriteLine("STOP REQQ");
//jobA.RequestStop();
//Console.WriteLine("STATE: " + jobA.jobContext.State);
//jobA.RequestResource(a);
//Thread.Sleep(1000);
//jobB.RequestResource(a);
//Thread.Sleep(1000);
//Console.WriteLine("A=" + jobA.GetJobContext().Priority);
//Console.WriteLine("B=" + jobB.GetJobContext().Priority);
//Console.WriteLine("RELEASE CALLED!");
//jobA.ReleaseResource(a);
//var path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
//Bitmap image = (Bitmap)System.Drawing.Image.FromFile(path);


string path = "Images/InputImages/";
string path2 = "Images/II/";
//Bitmap image = (Bitmap)System.Drawing.Image.FromFile(path + "cat.jpg");
string outputPath = "Images/OutputImages/";
string outputPath2 = "Images/OutputImages2/";
List<(string, string)> tupple = new List<(string, string)>();
tupple.Add((path, outputPath));
//tupple.Add((path2, outputPath2));

List<string> inputPaths = new();
inputPaths.Add(path);
//inputPaths.Add(path2);

/*Job demoJob = taskScheduler.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
{
    Name = "Job A",
    Parallelism = 1,
    SingleParallelism = 2
})
{ Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) });*/
Job demoJob2 = taskScheduler.AddJobWithScheduling(new JobSpecification(new NormalizeImageJob(inputPaths, outputPath)
{
    Name = "Job A",
    Parallelism = 1,
    SingleParallelism = 1
})
{ Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) });
//Thread.Sleep(2000);
//demoJob.RequestStop();

/*AbstractScheduler prioritySchedulerPreemptive2;
prioritySchedulerPreemptive2 = new PrioritySchedulerNoPreemption()
{
    MaxConcurrentTasks = 2
};

ResourceClass a1 = new ResourceClass("R1");
Job jobA1 = prioritySchedulerPreemptive2.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 5,
    SleepTime = 1000
})
{ Priority = 2 });
Job jobB1 = prioritySchedulerPreemptive2.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 5,
    SleepTime = 1000
})
{ Priority = 1 });
jobA1.RequestResource(a1);
Thread.Sleep(200);
prioritySchedulerPreemptive2.ScheduleUnscheduledJob(jobB1);
Thread.Sleep(200);
jobB1.RequestResource(a1);
HashSet<TaskScheduler.ResourceClass> resources = new HashSet<TaskScheduler.ResourceClass>();*/
//prioritySchedulerPreemptive2.resourceMap.TryGetValue(jobA.GetJobContext(), out resources);
//Assert.IsTrue(jobA.GetJobContext().Priority > jobB.GetJobContext().Priority);




