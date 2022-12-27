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
    public class PriorityQueue : ObservableCollection<JobContext>, AbstractQueue
    {
        //private PriorityQueue<JobContext, int> queue = new();
        //private int priority;
        public PriorityQueue()
        {
        }

        /*internal override void Enqueue(JobContext jobContext, int priority)
        {
            queue.Enqueue(jobContext, priority);
        }

        internal override JobContext Dequeue()
        {
            return queue.Dequeue();
        }*/

        /*private readonly Comparison<JobContext> comparison;

        

        public PriorityQueue(Comparison<JobContext> comparison)
        {
            this.comparison = comparison;
        }*/

        /*public void Enqueue(JobContext item, int priority)
        {
            int index = Items.TakeWhile(x => comparison(x, item) < 0).Count();
            Insert(index, item);
        }*/

        public void Enqueue(JobContext item, int priority)
        {
            int index = Items.TakeWhile(x => CompareInts(x.Priority, item.Priority) < 0).Count();
            Insert(index, item);
        }

        public JobContext Dequeue()
        {
            JobContext item = Items[0];
            RemoveAt(0);
            return item;
        }

        private static int CompareInts(int x, int y)
        {
            return x - y;
        }

        public int Count()
        {
            return Items.Count;
        }

        /*public void Enqueue(JobContext jobContext, int priority)
        {
            throw new NotImplementedException();
        }*/
    }
}
