using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskScheduler.Graph
{
    //CODE COPIED FROM "FORMALNE METODE"
    public class DeadlockDetectionGraph
    {
        public int size;
        public int[,] ms { get; set; }
        public int[] nodes { get; set; }

        //public DeadlockDetectionGraph() { }

        //Initialize empty graph
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
            
            for (int i = 0; i < size; i++, Console.WriteLine())
            {
                Console.Write("NODE: " + nodes[i]);
                for (int j = 0; j < size; j++)
                {
                    Console.Write(ms[i, j] + " ");
                }
            }
        }

        //Fucntion to check if there is a cycle
        //If there was a node value in set before and we found that value again -> cycle
        public bool DFSForCycleCheck(int startState)
        {
            //Stack<int> stack = new Stack<int>();
            
            bool isThereACycle = false;
            List<int> set = new();
            bool[] visit = InitVisit();

            void dfs_visit(int u)
            {
                int v;
                visit[u] = true;

                //CYCLE FOUND
                if (set.Contains(nodes[u]))
                {
                    isThereACycle = true;
                    return;
                }
                set.Add(nodes[u]);

                for (v = 0; v < size; v++)
                {
                    if (ms[u, v] == 1 && (!visit[v] || nodes[v] == startState))
                    {
                        dfs_visit(v);
                    }
                }
            }

            for (int i = 0; i < size; i++)
            {
                if (startState == nodes[i])
                {
                    dfs_visit(i);
                }
                /*else
                {
                    throw new InvalidOperationException("Can't start DFS traversal with a non-existing state!");
                }*/
            }

            return isThereACycle;
        }

        private bool[] InitVisit()
        {
            bool[] visit = new bool[size];
            for (int i = 0; i < size; i++)
            {
                visit[i] = false;
            }

            return visit;
        }

    }
}
