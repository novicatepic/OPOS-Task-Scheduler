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
        public DateTime StartTime { get; set; } = new DateTime(2010, 01, 01);       //Time specifying when the job will be started
        public int MaxExecutionTime{ get; set; } = 0;      //Max time for the exectuion of a job, max execution time
        public DateTime FinishTime { get; set; } = new DateTime(2010, 01, 01);       //Time specifying when the job will be finished or stopped
        //public Boolean IsSeparate { get; set; } = false;
        public JobSpecification(IUserJob userJob)
        {
            UserJob = userJob;
        }

        
    }
}
