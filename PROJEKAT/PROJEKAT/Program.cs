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
    MaxConcurrentTasks = 2
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
{ Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45)});

Job jobB = taskScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 10,
    SleepTime = 1000
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
    NumIterations =10,
    SleepTime = 500
})
{ Priority = 0} );

Resource a = new Resource("R1");
Resource b = new Resource("R2");
Resource c = new Resource("R3");
Thread.Sleep(1000);
jobA.RequestResource(a);
Thread.Sleep(1000);
jobB.RequestResource(a);
Thread.Sleep(500);
Console.WriteLine("RELEASE CALLED!");
//jobA.ReleaseResource(a);
//var path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
//Bitmap image = (Bitmap)System.Drawing.Image.FromFile(path);


/*string path = "Images/InputImages/";
string path2 = "Images/II/";
Bitmap image = (Bitmap)System.Drawing.Image.FromFile(path + "cat.jpg");
string outputPath = "Images/OutputImages/";
string outputPath2 = "Images/OutputImages2/";
List<(string, string)> tupple = new List<(string, string)>();
tupple.Add((path, outputPath));
tupple.Add((path2, outputPath2));

Job demoJob = taskScheduler.AddJobWithScheduling(new JobSpecification(new ImageNormalizationJob(tupple)
{
    Name = "Job A",
    parallelism = 1
})
{ Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45) }); ;
Thread.Sleep(2000);
demoJob.RequestStop();*/




