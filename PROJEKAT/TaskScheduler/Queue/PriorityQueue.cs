using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    internal class PriorityQueue : AbstractQueue
    {
        private PriorityQueue<JobContext, int> queue = new();

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

        public PriorityQueue<JobContext, int> GetQueue()
        {
            return queue;
        }
    }
}
