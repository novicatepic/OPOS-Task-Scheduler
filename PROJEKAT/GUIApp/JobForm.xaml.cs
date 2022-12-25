using PROJEKAT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaskScheduler;
using static System.Net.Mime.MediaTypeNames;

namespace GUIApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class JobForm : Window
    {
        //Default parameters, jff
        private string jobName = "";
        private int priority = 0;
        private DateTime startDate = new DateTime(2010, 1, 1);
        private DateTime endDate = new DateTime(2010, 1, 1);
        private int maxExecTime = 0;
        private int startYear = 0;
        private int startMonth = 0;
        private int startDay = 0;
        private int startHours = 0;
        private int startMinutes = 0;
        private int startSeconds = 0;
        private int endYear = 0;
        private int endMonth = 0;
        private int endDay = 0;
        private int endHours = 0;
        private int endMinutes = 0;
        private int endSeconds = 0;
        private int sleepTime = 1000;
        private int numIterations = 3;
        private int iterations = 0;
        private JobSpecification jobSpecification = null;

        MainWindow mainWindow;

        public JobForm(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            //checkBoxYes.IsChecked = false;
            //checkBoxNo.IsChecked = false;
        }

        //JobName
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (jobNameBox != null && jobNameBox.ToString() != "")
            {
                jobName = jobNameBox.ToString();
            }
        }

        private void jobPriorityBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(jobPriorityBox.Text, out priority);
        }

        private void numIterationsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(numIterationsBox.Text, out numIterations);
        }

        private void sleepTimeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(sleepTimeBox.Text, out sleepTime);
            sleepTime *= 1000;
        }

        private void startDateBox_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var tmp = startDateBox.SelectedDate;
            startYear = tmp.Value.Year;
            startMonth = tmp.Value.Month;
            startDay = tmp.Value.Day;
        }

        private void startHour_TextChanged(object sender, TextChangedEventArgs e)
        {
            //startDate.Hour = Int32.TryParse(startHourBox);
            Int32.TryParse(startHourBox.Text, out startHours);
        }

        private void startMinutes_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(startMinutesBox.Text, out startMinutes);
        }

        private void startSeconds_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(startSecondsBox.Text, out startSeconds);
        }

        private void finishDateBox_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var tmp = finishDateBox.SelectedDate;
            endYear = tmp.Value.Year;
            endMonth = tmp.Value.Month;
            endDay = tmp.Value.Day;
        }

        private void finishHoursBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(finishHoursBox.Text, out endHours);
        }

        private void finishMinutesBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(finishMinutesBox.Text, out endMinutes);
        }

        private void finishSecondsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(finishSecondsBox.Text, out endSeconds);
        }

        private void checkBoxYes_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBoxYes != null && checkBoxNo != null)
            {
                checkBoxYes.IsChecked = true;
                checkBoxNo.IsChecked = false;
            }

        }

        //Unnecessary for now!
        private void checkBoxNo_Checked(object sender, RoutedEventArgs e)
        {
            if (checkBoxYes != null && checkBoxNo != null)
            {
                checkBoxYes.IsChecked = false;
                checkBoxNo.IsChecked = true;
            }
        }

        private void checkBoxNo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkBoxYes != null && checkBoxNo != null)
            {
                checkBoxYes.IsChecked = true;
                checkBoxNo.IsChecked = false;
            }
        }

        private void checkBoxYes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (checkBoxYes != null && checkBoxNo != null)
            {
                checkBoxYes.IsChecked = false;
                checkBoxNo.IsChecked = true;
            }
        }

        /*private void confirmButton_Click(object sender, RoutedEventArgs e)
        {

            startDate = new DateTime(startYear, startMonth, startDay, startHours, startMinutes, startSeconds);
            endDate = new DateTime(endYear, endMonth, endDay, endHours, endMinutes, endSeconds);
            jobSpecification = new JobSpecification(new DemoUserJob()
            {
                Name = jobName,
                NumIterations = numIterations,
                SleepTime = sleepTime
            })
            { Priority = priority, StartTime = startDate, FinishTime = endDate, MaxExecutionTime = maxExecTime };
            if(GetStartJob())
            {
                MainWindow.taskScheduler.AddJobWithScheduling(jobSpecification);
            } 
            else
            {
                MainWindow.taskScheduler.AddJobWithoutScheduling(jobSpecification);
            }
        }*/

        public async void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            //Task<JobSpecification> taskJobSpec = new Task<>()
            //JobSpecification[] result = new JobSpecification[1];
            startDate = new DateTime(startYear, startMonth, startDay, startHours, startMinutes, startSeconds);
            endDate = new DateTime(endYear, endMonth, endDay, endHours, endMinutes, endSeconds);
            /*jobSpecification = new JobSpecification(new DemoUserJob()
            {
                Name = this.jobName,
                NumIterations = numIterations,
                SleepTime = sleepTime
            })
            { Priority = priority, StartTime = startDate, FinishTime = endDate, MaxExecutionTime = maxExecTime };*/
            if (GetStartJob())
            {
                mainWindow.taskScheduler.AddJobWithScheduling(jobSpecification);
            }
            else
            {
                mainWindow.taskScheduler.AddJobWithoutScheduling(jobSpecification);
            }
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            TextBlock jobName = new TextBlock();
            jobName.Text = this.jobName;
            jobName.Width = 192;
            jobName.FontSize = 12;
            jobName.TextAlignment = TextAlignment.Center;
            //TextAlignment = "Center" Text = "Demo job" Width = "192" FontSize = "12"
            //< TextBlock TextAlignment = "Center" Text = "5" Width = "65" FontSize = "12" ></ TextBlock >
            TextBlock jobPriority = new TextBlock();
            jobPriority.Text = this.priority.ToString();
            jobPriority.TextAlignment = TextAlignment.Center;
            jobPriority.Width = 65;
            jobPriority.FontSize = 12; 
            ProgressBar progressBar = new ProgressBar();
            //< ProgressBar x: Name = "pB" Width = "400" ></ ProgressBar >
            progressBar.Width = 400;
            TextBlock jobSt = new TextBlock();
            jobSt.Text = "Not started";
            //< TextBlock TextAlignment = "Center" Text = "Waiing to resume" Width = "100" FontSize = "12" ></ TextBlock >
            jobSt.TextAlignment = TextAlignment.Center;
            jobSt.Width = 100;
            jobSt.FontSize = 12;
            sp.Children.Add(jobName);
            sp.Children.Add(jobPriority);
            sp.Children.Add(progressBar);
            sp.Children.Add(jobSt);
            StackPanel buttonPanel = new StackPanel();
            buttonPanel.Orientation = Orientation.Horizontal;
            Button startButton = new Button();
            startButton.Name = "start";
            startButton.Content = "Start";
            Button pauseButton = new Button();
            pauseButton.Name = "pause";
            pauseButton.Content = "Pause";
            Button continueButton = new Button();
            continueButton.Name = "continue";
            continueButton.Content = "Continue";
            Button stopButton = new Button();
            stopButton.Name = "stop";
            stopButton.Content = "Stop";
            Button removeButton = new Button();
            removeButton.Name = "remove";
            removeButton.Content = "Remove";
            buttonPanel.Children.Add(startButton);
            buttonPanel.Children.Add(pauseButton);
            buttonPanel.Children.Add(continueButton);
            buttonPanel.Children.Add(stopButton);
            buttonPanel.Children.Add(removeButton);
            sp.Children.Add(buttonPanel);
            mainWindow.MainPanel.Children.Add(sp);
            //result[0] = jobSpecification; 
            //return result;
        }

        private bool GetStartJob()
        {
            if (checkBoxYes != null)
            {
                return (bool)checkBoxYes.IsChecked;
            }
            return false;
        }

        private void maxExecutionTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            Int32.TryParse(maxExecutionTimeBox.Text, out maxExecTime);
        }


    }
}
