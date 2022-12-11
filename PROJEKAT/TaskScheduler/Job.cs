namespace TaskScheduler
{
    public class Job
    {
        //Public Class Job is going to use JobContext methods
        //So user doesn't know what is really happening

        private readonly JobContext jobContext;

        internal Job(JobContext jobContext)
        {
            this.jobContext = jobContext;
        }

        public void WaitAll()
        {
            jobContext.WaitAll();
        }

        public void Wait(Job job)
        {
            jobContext.Wait(job.jobContext);
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

        internal JobContext GetJobContext()
        {
            return jobContext;
        }

    }
}