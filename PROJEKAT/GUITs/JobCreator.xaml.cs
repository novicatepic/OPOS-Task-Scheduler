using PROJEKAT.Jobs;
using System;
using System.Collections.Generic;
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
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            int priority = Int32.Parse(jobPriorityBox.ToString());
            int executionTime = Int32.Parse(maxExecutionTimeBox.ToString());

            var tmp = startDateBox.SelectedDate;
            int startYear = tmp.Value.Year;
            int startMonth = tmp.Value.Month;
            int startDay = tmp.Value.Day;

            

            string[] parseStartTime = startTimeBox.ToString().Split('-');
            if(parseStartTime.Length != 3)
            {
                throw new InvalidOperationException("Start time not correct!");
            }

            int startHours = Int32.Parse(parseStartTime[0]);
            int startMinutes = Int32.Parse(parseStartTime[1]);
            int startSeconds = Int32.Parse(parseStartTime[2]);

            DateTime startTime = new DateTime(startYear, startMonth, startDay, startHours, startMinutes, startSeconds);

            var tmp2 = endDateBox.SelectedDate;
            int endYear = tmp2.Value.Year;
            int endMonth = tmp2.Value.Month;
            int endDay = tmp2.Value.Day;

            string[] parseEndTime = endDateBox.ToString().Split('-');
            if(parseEndTime.Length != 3)
            {
                throw new InvalidOperationException("End time not correct!");
            }

            int endHours = Int32.Parse(parseEndTime[0]);
            int endMinutes = Int32.Parse(parseEndTime[1]);
            int endSeconds = Int32.Parse(parseEndTime[2]);

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
            



            if(shouldStart)
            {
                
            }
            else
            {

            }

        }
    }
}
