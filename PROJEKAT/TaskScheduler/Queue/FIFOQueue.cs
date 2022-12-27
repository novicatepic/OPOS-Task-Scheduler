using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    internal class FIFOQueue : /*ObservableCollection<JobContext>,*/ AbstractQueue
    {
        //protected ObservableHashSet<JobContext> queue = new();
        //private ObservableQueue<JobContext> queue = new();

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
