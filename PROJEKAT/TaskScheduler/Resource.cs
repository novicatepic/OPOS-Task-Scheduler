using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    internal class Resource
    {
        private string name;
        private static int uniqueID = 0;

        public Resource(string name)
        {
            this.name = name;
            this.name += uniqueID;
            uniqueID++;
        }

        public string GetName()
        {
            return name;
        }
    }
}
