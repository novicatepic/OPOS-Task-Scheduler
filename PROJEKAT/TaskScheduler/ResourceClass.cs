using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class ResourceClass
    {
        //Name has to be unique
        private string name;
        //private int id;
        //private static int uniqueID = 1;
        //private bool locked = false;

        public ResourceClass(string name)
        {
            this.name = name;
            //this.name += id;
            //id = uniqueID++;
        }

        public string GetName()
        {
            return name;
        }
        //public static int GetUniqueID() { return uniqueID; }

    }
}
