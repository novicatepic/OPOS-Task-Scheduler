using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    //IJobContenxt, interface which is used by a concrete job to check for different types of times
    public interface IJobContext
    {
        //Names self explanatory
        public void CheckForPause();
        public void CheckForStoppage();
        public bool StoppageConfirmed();
        public bool CheckExecutionTime();
        public bool CheckFinishTime();
        public void CheckForPriorityStoppage();
        public void SetJobTime(long time);
        public void CheckSliceStoppage();

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
