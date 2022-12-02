namespace TaskScheduler
{
    public class Job
    {
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
    }
}