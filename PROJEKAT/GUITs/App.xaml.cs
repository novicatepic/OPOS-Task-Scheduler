using PROJEKAT.Jobs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GUITs
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public static string dir = "./SerializeJobs/";
        public static string filename = "ser";
        private static int pathId = 0;

        private void Application_Exit(object sender, EventArgs e)
        {
            if(!Directory.Exists(dir)) 
            {
                Directory.CreateDirectory(dir);
            }

            if(JobCreator.aScheduler.guiJobs != null)
            {
                foreach (var job in JobCreator.aScheduler.guiJobs)
                {
                    if (job.jobState != TaskScheduler.JobContext.JobState.Finished && job.jobState != TaskScheduler.JobContext.JobState.Stopped)
                    {
                        pathId++;
                        string fName = filename + pathId;

                        try
                        {
                            NormalizeImageJob imageJob = (NormalizeImageJob)job.userJob;
                            using (FileStream fs = File.Create(dir + fName + ".txt")) { }

                            string toWrite = imageJob.Name + "\n" + imageJob.OutputPath + "\n";
                            foreach (var inputPath in imageJob.InputPaths)
                            {
                                toWrite += inputPath + ">";
                            }
                            toWrite += "\n";
                            toWrite += imageJob.imageCounter + "\n";
                            toWrite += imageJob.SleepTime + "\n";
                            toWrite += imageJob.Parallelism + "\n";
                            toWrite += imageJob.SingleParallelism;

                            string[] arrayString = new string[1];
                            arrayString[0] = toWrite;

                            File.WriteAllLines(dir + fName + ".txt", arrayString);

                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            //MessageBox.Show(JobCreator.aScheduler.guiJobs.ElementAt(0).id.ToString());
        }
    }
}
