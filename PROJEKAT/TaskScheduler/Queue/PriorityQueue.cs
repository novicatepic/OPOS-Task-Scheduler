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
    }
}
