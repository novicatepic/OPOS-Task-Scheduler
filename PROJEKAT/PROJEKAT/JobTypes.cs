using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PROJEKAT
{
    public class JobTypes
    {
        public static Type[] GetJobTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes();
        }
    }
}
