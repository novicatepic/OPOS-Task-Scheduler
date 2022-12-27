using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    //IJobContenxt only has CheckForPause method, for now
    public interface IJobContext
    {
        public void CheckForPause();
        public void CheckForStoppage();
        public bool StoppageConfirmed();
        public bool CheckExecutionTime();
        public bool CheckFinishTime();
        public void CheckForPriorityStoppage();

        public void CheckSliceStoppage();
        //public void SetFinished();

        //public void CallFinish();
        public void CheckForResourse();

        public bool CheckConditions()
        {
            if(CheckExecutionTime() || CheckFinishTime())
            {
                return true;
            }
            return false;
        }

        public void CheckAll()
        {
            CheckForPause();

            
            CheckForStoppage();

            CheckForPriorityStoppage();

            CheckSliceStoppage();

            CheckForResourse();
            
        }

        public abstract void SetProgress(double progress);
    }
}
