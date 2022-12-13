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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskScheduler;

namespace GUIApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal static TaskScheduler.TaskScheduler taskScheduler = null;



        public MainWindow()
        {
            InitializeComponent();
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            JobForm jobForm = new JobForm();
            jobForm.Show();
        }

        //FIFO scheduling selected
        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            taskScheduler = new TaskScheduler.TaskScheduler(fifoflag: true);
            algorithmComboBox.IsEnabled = false;
            createButton.IsEnabled = true;
        }

        //Priority scheduling selected
        private void ComboBoxItem_Selected_1(object sender, RoutedEventArgs e)
        {
            taskScheduler = new TaskScheduler.TaskScheduler(fifoflag: false);
            algorithmComboBox.IsEnabled = false;
            createButton.IsEnabled = true;
        }

        private void numTasksBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(taskScheduler != null)
            {
                taskScheduler.MaxConcurrentTasks = Int32.Parse(numTasksBox.Text);
            }          
        }
    }
}
