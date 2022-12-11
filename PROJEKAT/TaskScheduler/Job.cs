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

        //Wait on other jobs to finish without letting another job get time to execute on the processor
        public void Wait()
        {
            jobContext.Wait();
        }

        //Wait on another job to execute and leave thread, after that go to the waiting queue
        public void Wait(Job job)
        {
            //jobContext.Wait();
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