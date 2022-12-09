using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    internal class PriorityQueue : AbstractQueue
    {
        private PriorityQueue<JobContext, int> queue = new();
        //private int priority;

        public PriorityQueue()
        {
            //this.priority = priority;
        }

        protected internal override void Enqueue(JobContext jobContext, int priority)
        {
            queue.Enqueue(jobContext, priority);
        }

        protected internal override JobContext Dequeue()
        {
            return queue.Dequeue();
        }

        protected internal override int Count()
        {
            return queue.Count;
        }
    }
}
