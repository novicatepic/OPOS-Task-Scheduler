using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    //User Job interface has Run method which only takes IJobContext as a parameter
    //And IJobContext only has CheckForPause method
    public interface IUserJob
    {
        protected internal void Run(IJobContext jobApi);

        public int parallelism { get; init; }
    }
}
