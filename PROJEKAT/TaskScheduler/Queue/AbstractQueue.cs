using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskScheduler.Scheduler;

namespace TaskScheduler.Queue
{
    internal abstract class AbstractQueue
    {
        //protected ObservableHashSet<JobContext> queue = new();
        internal abstract void Enqueue(JobContext jobContext, int priority);
        internal abstract JobContext Dequeue();

        internal abstract int Count();

    }
}
