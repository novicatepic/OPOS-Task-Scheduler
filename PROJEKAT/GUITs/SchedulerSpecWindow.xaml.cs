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

namespace GUITs
{
    /// <summary>
    /// Interaction logic for SchedulerSpecWindow.xaml
    /// </summary>
    /// 





    public partial class SchedulerSpecWindow : Window
    {
        public SchedulerSpecWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string whichAlgorithm;
            int parallelismLevel = Int32.Parse(parralelismBox.Text); 
            if(rb1.IsChecked== true ) { whichAlgorithm = "FIFO"; }
            else if(rb2.IsChecked == true) { whichAlgorithm = "RR";  }
            else if (rb3.IsChecked == true) { whichAlgorithm = "NoPreemption"; }
            else if (rb4.IsChecked == true) { whichAlgorithm = "NoPreemptionSlice"; }
            else if (rb5.IsChecked == true) { whichAlgorithm = "Preemption"; }
            else if (rb6.IsChecked == true) { whichAlgorithm = "PreemptionSlice"; }
            else { throw new Exception("Algorithm not picked, scheduler can't be started!"); }

            MainWindow mainWindow= new MainWindow(whichAlgorithm, parallelismLevel);
            mainWindow.Show();
            this.Close();

        }
    }
}
