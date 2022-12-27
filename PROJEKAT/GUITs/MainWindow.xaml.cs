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
        public MainWindow(string algorithm, int numOfConcurrentTasks)
        {
            InitializeComponent();       
            

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

            RunningJobs.ItemsSource = abstractScheduler.guiJobs;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JobCreator jobCreator = new(abstractScheduler);
            jobCreator.Show();
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
    }
}
