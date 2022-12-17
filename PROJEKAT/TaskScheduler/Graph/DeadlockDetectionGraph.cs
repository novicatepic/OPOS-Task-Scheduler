using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Graph
{
    public class DeadlockDetectionGraph
    {
        private int size;
        public int[,] ms { get; set; }
        public int[] nodes { get; set; }

        //public DeadlockDetectionGraph() { }

        public DeadlockDetectionGraph(int size)
        {
            this.size = size;
            ms = new int[size, size];
            nodes = new int[size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    ms[i, j] = 0;
                }
            }

        }

        public int FindPositionOfState(int state)
        {
            for (int i = 0; i < size; i++)
            {
                if (nodes[i] == state)
                    return i;
            }
            return -1;
        }

        public void PrintMatrix()
        {
            for(int i = 0; i < size; i++, Console.WriteLine())
            {
                for(int j = 0; j < size; j++)
                {
                    Console.Write(ms[i, j] + " ");
                }
            }
        }

    }
}
