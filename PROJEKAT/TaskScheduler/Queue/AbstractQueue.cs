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
    public interface AbstractQueue
    {
        //protected ObservableHashSet<JobContext> queue = new();
        public void Enqueue(JobContext jobContext, int priority);
        public JobContext Dequeue();

        public int Count();

    }
}
