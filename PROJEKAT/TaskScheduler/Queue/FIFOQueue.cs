using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    public class FIFOQueue : AbstractQueue
    {
        private Queue<JobContext> queue = new();

        internal override void Enqueue(JobContext jobContext, int priority)
        {
            queue.Enqueue(jobContext);
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
