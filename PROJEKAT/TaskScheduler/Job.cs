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

        public void Wait()
        {
            jobContext.Wait();
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

    }
}