using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    public class PriorityQueue : AbstractQueue
    {
        private PriorityQueue<JobContext, int> queue = new();
        //private int priority;
        private bool withPreemption = false;
        private bool timeSlicing = false;

        public PriorityQueue()
        {
        }

        internal override void Enqueue(JobContext jobContext, int priority)
        {
            queue.Enqueue(jobContext, priority);
        }

        internal override JobContext Dequeue()
        {
            return queue.Dequeue();
        }

        internal override int Count()
        {
            return queue.Count;
        }

        public bool GetWithPreemption()
        {
            return withPreemption;
        }

        public void SetWithPreemption(bool withPreemption)
        {
            if(timeSlicing)
            {
                throw new InvalidOperationException("Can't set both time slicing and preemptive");
            }
            this.withPreemption = withPreemption;
        }

        public bool GetWithTimeSlicing()
        {
            return timeSlicing;
        }

        public void SetWithTimeSlicing(bool timeSlicing)
        {
            if(withPreemption)
            {
                throw new InvalidOperationException("Can't set both time slicing and preemptive");
            }
            this.timeSlicing = timeSlicing;
        }
    }
}
