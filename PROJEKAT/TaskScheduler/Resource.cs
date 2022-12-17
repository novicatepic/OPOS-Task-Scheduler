using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler
{
    public class Resource
    {
        private string name;
        private int id;
        private static int uniqueID = 1;
        //private bool locked = false;

        public Resource(string name)
        {
            this.name = name;
            this.name += id;
            id = uniqueID++;
        }

        public string GetName()
        {
            return name;
        }

        /*public void LockResource()
        {
            locked = true;
        }*/

        /*public bool IsLocked()
        {
            return locked;
        } */
        
        public static int GetUniqueID() { return uniqueID; }

    }
}
