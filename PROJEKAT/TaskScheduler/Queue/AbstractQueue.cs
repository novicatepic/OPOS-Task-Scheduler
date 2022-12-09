using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Queue
{
    abstract internal class AbstractQueue
    {
        abstract protected internal void Enqueue(JobContext jobContext, int priority);
        abstract protected internal JobContext Dequeue();

        abstract protected internal int Count();
    }
}
