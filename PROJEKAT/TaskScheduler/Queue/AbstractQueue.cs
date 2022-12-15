using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    abstract public class AbstractQueue
    {
        abstract internal void Enqueue(JobContext jobContext, int priority);
        abstract internal JobContext Dequeue();

        abstract internal int Count();
    }
}
