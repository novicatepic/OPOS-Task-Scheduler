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
    }
}
