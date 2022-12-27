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
    public class FIFOQueue : ObservableCollection<JobContext>, AbstractQueue
    {
        protected ObservableHashSet<JobContext> queue = new();
        //private ObservableQueue<JobContext> queue = new();

        public void Enqueue(JobContext item, int priority)
        {
            //queue.
            //queue.Remove(queue.ElementAt(2));
            //queue.Add(jobContext);
            //queue.Enqueue(jobContext);
            Insert(Items.Count, item);
        }

        public JobContext Dequeue()
        {
            //return queue.Dequeue();
            /*JobContext returnJob = queue.ElementAt(0);
            queue.Remove(queue.ElementAt(0));
            return returnJob;*/
            JobContext item = Items[0];
            RemoveAt(0);
            return item;
        }

        public int Count()
        {
            return Items.Count;
        }
    }
}
