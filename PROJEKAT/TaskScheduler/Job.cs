
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using static TaskScheduler.JobContext;

namespace TaskScheduler
{
    public class Job : INotifyPropertyChanged
    {
        //Public Class Job is going to use JobContext methods
        //So user doesn't know what is really happening

        private readonly JobContext jobContext;
        private bool isSeparate = false;
        public int id { get; set; }

        //public Job() { }

        internal Job(JobContext jobContext)
        {
            this.id = jobContext.id;
            this.jobContext = jobContext;
            //jobContext.SetProgressHandler(HandleProgressMade);
        }

        public Job(JobSpecification jobSpecification)
        {
            JobContext jobContext = new(
                userJob: jobSpecification.UserJob,
                priority: jobSpecification.Priority,                //priority = jobs priority
                startTime: jobSpecification.StartTime,
                finishTime: jobSpecification.FinishTime,
                maxExecutionTime: jobSpecification.MaxExecutionTime,
                isSeparate: true);

            //jobContext.SetProgressHandler(HandleProgressMade);

            this.jobContext = jobContext;
            isSeparate = true;
        }

        private double progress;
        public double Progress
        {
            get
            {
                return progress;
            }

            private set
            {
                progress = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /*private void HandleProgressMade(double setProgress)
        {
             Progress = setProgress;
        }*/

        public bool IsStartable
        {
            get
            {
                lock (jobContext.jobContextLock)
                {
                    return jobContext.jobState == JobState.NotStarted || jobContext.jobState == JobState.NotScheduled
                        || jobContext.jobState == JobState.Paused;
                }
            }
        }

        public bool IsCloseable
        {
            get
            {
                //lock(jobContext.jobContextLock)
                //{
                    return jobContext.jobState == JobState.Finished;
                //}
            }
        }

        public bool IsPausable
        {
            get
            {
                //lock(jobContext.jobContextLock)
                //{
                return jobContext.jobState == JobState.Running;
                //}
            }
        }

        public bool IsStoppable
        {
            get
            {
                return jobContext.jobState == JobState.Running;
            }
        }

        //Wait on other jobs to finish without letting another job get time to execute on the processor
        public void Wait()
        {
            if(!isSeparate)
            {
                jobContext.Wait();
            } else
            {
                throw new InvalidOperationException("This is started as a separate process, therefore doesn't wait for anyone!");
            }
            
        }

        //Wait on another job to execute and leave thread, after that go to the waiting queue
        public void Wait(Job job)
        {
            if(!isSeparate)
            {
                jobContext.Wait(job.jobContext);
            }
            else
            {
                throw new InvalidOperationException("This is started as a separate process, therefore doesn't know about other threads!");
            }
            
        }

        public void RequestPause()
        {
            jobContext.RequestPause();
        }

        public void RequestContinue()
        {
            jobContext.RequestContinue();   
        }

        public void RequestStop()
        {
            jobContext.RequestStop();
        }

        public void StartJobAsSeparateProcess()
        {
            jobContext.Start();
        }

        public void RequestResource(Resource resource)
        {
            jobContext.RequestResource(resource);
        }

        public void RequestResources(HashSet<Resource> resources)
        {
            jobContext.RequestResources(resources);
        }

        public void ReleaseResource(Resource resource)
        {
            jobContext.ReleaseResource(resource);
        }

        public void ReleaseResources(HashSet<Resource> resources)
        {
            jobContext.ReleaseResources(resources);
        }

        internal JobContext GetJobContext()
        {
            return jobContext;
        }

    }
}