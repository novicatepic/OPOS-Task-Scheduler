using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Scheduler
{
    public interface IPreemption
    {
        public void CheckPreemption(JobContext jobContext);
    }
}
