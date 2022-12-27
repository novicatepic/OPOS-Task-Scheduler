using PROJEKAT;
using PROJEKAT.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
using TaskScheduler.Scheduler;

namespace GUITs
{
    /// <summary>
    /// Interaction logic for JobCreator.xaml
    /// </summary>
    public partial class JobCreator : Window
    {
        internal AbstractScheduler abstractScheduler;

        public JobSpecification? jobContext;

        /*public JobCreator()
        {
            InitializeComponent();
        }*/

        public JobCreator(AbstractScheduler abstractScheduler)
        {
            InitializeComponent();
            this.abstractScheduler = abstractScheduler;

            Type[] types = JobTypes.GetJobTypes();
            
            List<Type> tips = new();

            foreach(Type type in types)
            {
                if (type.GetInterfaces().Contains(typeof(IUserJob))) 
                {
                    tips.Add(type);
                }
            }
            IEnumerable<Type> derivedTypes = tips;

            JobTypeListBox.ItemsSource = derivedTypes;
            JobTypeListBox.SelectedIndex = 0;

        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            int priority = 0, executionTime = 0;
            Int32.TryParse(jobPriorityBox.ToString(), out priority);
            Int32.TryParse(maxExecutionTimeBox.ToString(), out executionTime);

            var tmp = startDateBox.SelectedDate;
            int startYear = tmp.Value.Year;
            int startMonth = tmp.Value.Month;
            int startDay = tmp.Value.Day;

            

            string[] parseStartTime = startTimeBox.ToString().Split('-');
            if(parseStartTime.Length != 3)
            {
                throw new InvalidOperationException("Start time not correct!");
            }

            int startHours = 0, startMinutes = 0, startSeconds = 0;

            if (!"H".Equals(parseStartTime[0]))
            {
                Int32.TryParse(parseStartTime[0], out startHours);
                Int32.TryParse(parseStartTime[1], out startMinutes);
                Int32.TryParse(parseStartTime[2], out startSeconds);
            }
            

            DateTime startTime = new DateTime(startYear, startMonth, startDay, startHours, startMinutes, startSeconds);

            var tmp2 = endDateBox.SelectedDate;
            int endYear = tmp2.Value.Year;
            int endMonth = tmp2.Value.Month;
            int endDay = tmp2.Value.Day;

            string[] parseEndTime = endTimeBox.ToString().Split('-');
            if(parseEndTime.Length != 3)
            {
                throw new InvalidOperationException("End time not correct!");
            }

            int endHours = 0, endMinutes = 0, endSeconds = 0;

            if (!"H".Equals(parseEndTime[0]))
            {
                Int32.TryParse(parseEndTime[0], out endHours);
                Int32.TryParse(parseEndTime[1], out endMinutes);
                Int32.TryParse(parseEndTime[2], out endSeconds);
            }
            
            DateTime endTime = new DateTime(endYear, endMonth, endDay, endHours, endMinutes, endSeconds);

            bool shouldStart = true;

            if((bool)jobStartedRB.IsChecked)
            {
                shouldStart = true;
            }
            else if((bool)jobNotStartedRB.IsChecked)
            {
                shouldStart = false; 
            }


            JobSpecification jobSpecification = new JobSpecification();
            jobSpecification.StartTime = startTime;
            jobSpecification.FinishTime = endTime;
            jobSpecification.Priority = priority;
            jobSpecification.MaxExecutionTime = executionTime;

            Type jobType = (Type)JobTypeListBox.SelectedItem;
            IUserJob userJob = (IUserJob)Activator.CreateInstance(jobType);
            jobSpecification.UserJob = (IUserJob)JsonSerializer.Deserialize(JsonTextBox.Text, userJob.GetType(), jsonSerializerOptions);


            if(shouldStart)
            {
                abstractScheduler.AddJobWithScheduling(jobSpecification);
            }
            else
            {
                abstractScheduler.AddJobWithoutScheduling(jobSpecification);
            }

        }

        private void JobTypeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Type taskType = (Type)JobTypeListBox.SelectedItem;
            object userTask = Activator.CreateInstance(taskType);
            JsonTextBox.Text = JsonSerializer.Serialize(userTask, userTask.GetType(), jsonSerializerOptions);
        }

        private static readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
    }
}
