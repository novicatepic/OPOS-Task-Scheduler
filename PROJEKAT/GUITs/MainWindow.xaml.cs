using PROJEKAT.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskScheduler;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;

namespace GUITs
{

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AbstractScheduler abstractScheduler;
        //PriorityQueue queue2 = new();
        public MainWindow(string algorithm, int numOfConcurrentTasks)
        {
            InitializeComponent();

            ObservableCollection<JobContext> collection = new();
            ObservableCollection<JobContext> collection2 = new();
            ObservableCollection<JobContext> collection3 = new();

            //PriorityQueue queue2 = new PriorityQueue();
            JobContext job = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);

            

            HashSet<JobContext> jobs2 = new();


            


            switch(algorithm)
            {
                case "FIFO":
                    abstractScheduler = new FIFOScheduler() { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "RR":
                    abstractScheduler = new FIFOSchedulerSlicing(2000) { MaxConcurrentTasks = numOfConcurrentTasks };
                    break;
                case "NoPreemption":
                    abstractScheduler = new PrioritySchedulerPreemption() { MaxConcurrentTasks = numOfConcurrentTasks };
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

            BindingOperations.EnableCollectionSynchronization(abstractScheduler.runningJobs, abstractScheduler.schedulerLock);
            BindingOperations.EnableCollectionSynchronization((FIFOQueue)abstractScheduler.jobQueue, abstractScheduler.schedulerLock);
            RunningJobs.ItemsSource = abstractScheduler.runningJobs;
            WaitingJobs.ItemsSource = (FIFOQueue)abstractScheduler.jobQueue;

            /*Dispatcher.BeginInvoke(delegate // <--- HERE
            {
                await Task.Delay(500);
                abstractScheduler.runningJobs.Add(job);
            });*/

           

            Dispatcher.BeginInvoke(async () =>
            {
                abstractScheduler.AddJobWithScheduling(new JobSpecification(new DemoUserJob()));
                //var uiContext = SynchronizationContext.Current;
                //uiContext.Send(x => abstractScheduler.HandleJobFinished(job), null);
                abstractScheduler.runningJobs.Add(job);
                job.Priority = 4;
                await Task.Delay(1000);
                
                JobContext jo2 = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);
                abstractScheduler.runningJobs.Add(jo2);
                job.Priority = 2;
                await Task.Delay(1000);
                JobContext jo3 = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);
                abstractScheduler.runningJobs.Add(jo3);
                job.Priority = 1;
                await Task.Delay(1000);
                JobContext jo4 = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);
                abstractScheduler.runningJobs.Add(jo4);
                await Task.Delay(1000);
                //queue2.Add(job);
                //job.Progress = 50;
                await Task.Delay(1000);
                //queue2.Add(new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false));
            });
            //abstractScheduler.runningJobs.Remove(job);
            Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(1000);
                abstractScheduler.jobQueue.Enqueue(job, 0);
                await Task.Delay(1000);
                JobContext jo3 = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);
                abstractScheduler.jobQueue.Enqueue(jo3, 0);
                await Task.Delay(1000);
                JobContext jo4 = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);
                abstractScheduler.jobQueue.Enqueue(jo4, 0);
            });

        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //queue2.ElementAt(0)
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JobCreator jobCreator = new(abstractScheduler);
            jobCreator.Show();
        }
    }
}
