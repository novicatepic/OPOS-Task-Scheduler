
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using static TaskScheduler.JobContext;

namespace TaskScheduler
{
    public class Job
    {
        //Public Class Job is going to use JobContext methods
        //So user doesn't know what is really happening

        public readonly JobContext jobContext;
        private bool isSeparate = false;

        internal Job(JobContext jobContext)
        {
            this.jobContext = jobContext;
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

            this.jobContext = jobContext;
            isSeparate = true;
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

        public void RequestResource(ResourceClass resource)
        {
            jobContext.RequestResource(resource);
        }

        public void RequestResources(HashSet<ResourceClass> resources)
        {
            jobContext.RequestResources(resources);
        }

        public void ReleaseResource(ResourceClass resource)
        {
            jobContext.ReleaseResource(resource);
        }

        public void ReleaseResources(HashSet<ResourceClass> resources)
        {
            jobContext.ReleaseResources(resources);
        }

        public JobContext GetJobContext()
        {
            return jobContext;
        }

    }
}