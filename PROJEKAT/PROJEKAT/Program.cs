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

Job jobA = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job A",
    NumIterations = 30,
    SleepTime = 500
})
{ Priority = 3, StartTime = new DateTime(2022, 12, 18, 15, 27, 35), FinishTime = new DateTime(2022, 12, 12, 8, 0, 45)});

Job jobB = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job B",
    NumIterations = 3,
    SleepTime = 1000
})
{ Priority = 2 });

Job jobC = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job C",
    NumIterations = 15,
    SleepTime = 500
})
{ Priority = 1 });

Job jobX = taskScheduler.AddJobWithoutScheduling(new JobSpecification(new DemoUserJob()
{
    Name = "Job X",
    NumIterations = 7,
    SleepTime = 500
})
{ Priority = 0} );

/*Resource a = new Resource("R1");
Resource b = new Resource("R2");
Resource c = new Resource("R3");
Thread.Sleep(1000);
jobA.RequestResource(a);
Thread.Sleep(500);
taskScheduler.ScheduleUnscheduledJob(jobB);
Thread.Sleep(500);
jobB.RequestResource(a);
taskScheduler.ScheduleUnscheduledJob(jobC);
Thread.Sleep(4000);
Thread.Sleep(1000);
Thread.Sleep(1000);
Thread.Sleep(3000);
Console.WriteLine("RELEASE CALLED!");
jobA.ReleaseResource(a);*/
//var path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
//Bitmap image = (Bitmap)System.Drawing.Image.FromFile(path);


string path = "Images/InputImages/";
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
Thread.Sleep(1000);
//demoJob.RequestStop();
/*HashSet<int> s1 = new();

ObservableHashSet<int> queue = new();


queue.Add(1);
queue.Add(5);
queue.Add(3);
queue.Add(4);
queue.Add(2);

queue.Remove(3);

foreach(var elem in queue)
{
    Console.WriteLine(elem + " ");
}


PriorityQueue queue2 = new PriorityQueue();

queue2.Enqueue(jobA.jobContext, 0);
queue2.Enqueue(jobB.jobContext, 0);
queue2.Enqueue(jobC.jobContext, 0);
queue2.Enqueue(jobX.jobContext, 0);

foreach(var elem in queue2)
{
    Console.WriteLine(elem.Priority + " ");
}*/





