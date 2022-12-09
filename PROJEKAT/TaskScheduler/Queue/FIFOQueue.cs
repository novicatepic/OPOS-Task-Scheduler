using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    internal class FIFOQueue : AbstractQueue
    {
        private Queue<JobContext> queue = new();

        protected internal override void Enqueue(JobContext jobContext, int priority)
        {
            queue.Enqueue(jobContext);
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
