using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Queue;
using TaskScheduler.Scheduler;

namespace TaskScheduler.Scheduler
{
    internal class FIFOSchedulerSlicing : AbstractScheduler, ISlicer
    {

        
        public FIFOSchedulerSlicing()
        {
            jobQueue = new FIFOQueue();
        }

        internal override void ScheduleJob(JobContext jobContext)
        {
            throw new NotImplementedException();
        }

        void ISlicer.CheckSliceTime()
        {
            throw new NotImplementedException();
        }
    }
}
