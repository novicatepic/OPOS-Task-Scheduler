using PROJEKAT.Jobs;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TaskScheduler;
using TaskScheduler.Scheduler;

namespace GUITs
{

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AbstractScheduler abstractScheduler;
        public MainWindow(string algorithm, int numOfConcurrentTasks, int sliceTime)
        {
            InitializeComponent();
            //Application.ApplicationExit += new EventHandler(AppEvents.OnApplicationExit);

            //Whichever algorithm it is, it will be instantiated
            switch (algorithm)
            {
                case "FIFO":
                    abstractScheduler = new FIFOScheduler() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "RR":
                    abstractScheduler = new FIFOSchedulerSlicing(sliceTime) { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "NoPreemption":
                    abstractScheduler = new PrioritySchedulerNoPreemption() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "NoPreemptionSlice":
                    abstractScheduler = new PrioritySchedulerNoPreemptionSlicing() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "Preemption":                  
                    abstractScheduler = new PrioritySchedulerPreemption() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "PreemptionSlice":
                    abstractScheduler = new PrioritySchedulerPreemptionSlicing() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                default:
                    throw new Exception("EXCEPTION!");
            }

            RunningJobs.ItemsSource = abstractScheduler.guiJobs;

            //Deserialize from file logic
            //Basically, read files of serialized Image values and copy values from it, and restart it
            //Probably not this easy and shouldn't be done like this, but this is my implementation
            string[] filesToDeserialize = Directory.GetFiles("./SerializeJobs/");

            foreach(string file in filesToDeserialize)
            {
                string[] lines = File.ReadAllLines(file);
                NormalizeImageJob nJob = new NormalizeImageJob()
                {
                    Name = lines[0],
                    OutputPath = lines[1],
                    SingleParallelism = Int32.Parse(lines[lines.Length - 1]),
                    Parallelism = Int32.Parse(lines[lines.Length - 2]),
                    SleepTime = Int32.Parse(lines[lines.Length - 3]),
                    imageCounter = Int32.Parse(lines[lines.Length - 4])
                };
                nJob.InputPaths.Clear();
                String inputJ = lines[2];
                String[] splitJobs = inputJ.Split('>');
                foreach(var splitJob in splitJobs)
                {
                    if(!"".Equals(splitJob))
                    {
                        nJob.InputPaths.Add(splitJob);
                    }                   
                }
                JobSpecification jobSpec = new JobSpecification();
                jobSpec.UserJob = nJob;
                abstractScheduler.AddJobWithScheduling(jobSpec);
                File.Delete(file);
            }

            //Didn't find a way to check if user clicked Stop Debugging, don't know if it's even possible
            /*Thread helpThread;
            helpThread = new(() =>
            {
                while (System.Diagnostics.Debugger.IsAttached)
                {
                    MessageBox.Show("Attached");
                }
                MessageBox.Show("Deactivated!");
            });
            helpThread.Start();*/

        }

        //Code not complicated, based on button I chose option that user wanted to select
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JobCreator jobCreator = new(abstractScheduler);
            jobCreator.Show();
            //Application.ShutDown();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                JobContext job = (JobContext)button.DataContext;
                if(job.jobState == JobContext.JobState.Paused)
                {
                    job.RequestContinue();
                }
                else
                {
                    job.ExecuteJobManually();
                }
                
            }
        }


        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                JobContext job = (JobContext)button.DataContext;
                job.RequestPause();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                JobContext job = (JobContext)button.DataContext;
                job.RequestStop();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                JobContext job = (JobContext)button.DataContext;
                abstractScheduler.guiJobs.Remove(job);
            }
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            MessageBox.Show("yo!");
        }
    }
}
