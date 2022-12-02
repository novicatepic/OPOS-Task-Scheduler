using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public interface IUserJob
    {
        protected internal void Run(IJobContext jobApi);
    }
}
