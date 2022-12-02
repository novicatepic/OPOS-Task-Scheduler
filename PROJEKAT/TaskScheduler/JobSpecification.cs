using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class JobSpecification
    {
        internal IUserJob UserJob { get; }
        public int Priority { get; init; } = 0;

        public JobSpecification(IUserJob userJob)
        {
            UserJob = userJob;
        }
    }
}
