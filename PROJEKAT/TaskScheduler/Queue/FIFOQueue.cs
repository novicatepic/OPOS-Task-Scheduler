using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Services.WebApi.Jwt;
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

        internal Queue<JobContext> ReturnQueue() 
        {
            return queue;
        }

        internal void ReorderQueue(JobContext element)
        {
            bool before = true; bool after = false;
            Queue<JobContext> queueBefore = new();
            Queue<JobContext> queueAfter = new();
            for (int i = 0; i < queue.Count; i++)
            {
                if(before && element != queue.ElementAt(i))
                {
                    queueBefore.Enqueue(queue.ElementAt(i));
                }
                else
                {
                    before = false; 
                    after = true;
                }

                if(after && element != queue.ElementAt(i))
                {
                    queueAfter.Enqueue(queue.ElementAt(i));
                }
            }
            foreach(var elem in queueAfter)
            {
                queueBefore.Enqueue(elem);
            }

            queue.Clear();
            queue = queueBefore;
        }

    }
}
