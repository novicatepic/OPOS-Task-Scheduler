using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    //JobSpecification has IUserJob which has method Run which takes IJobContext and therefore can check for pause
    //IJobContext checks for pause
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
