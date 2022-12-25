using PROJEKAT.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
using TaskScheduler.Scheduler;

namespace GUITs
{

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        AbstractScheduler abstractScheduler;

        public MainWindow(string algorithm, int numOfConcurrentTasks)
        {
            InitializeComponent();

            ObservableCollection<JobContext> collection = new();
            ObservableCollection<JobContext> collection2 = new();

            //AbstractScheduler abstractScheduler = new FIFOScheduler();

            RunningJobs.ItemsSource = collection;
            WaitingJobs.ItemsSource = collection2;

            JobContext job = new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false);

            Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(1000);
                collection.Add(job);
                //job.Progress = 50;
                await Task.Delay(1000);
                collection.Add(new JobContext(new DemoUserJob(), 5, DateTime.Now, DateTime.Now, 0, false));
            });

            Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(1000);
                collection2.Add(job);
            });


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


        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
