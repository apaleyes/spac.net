using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spac.net.Chapter8
{
    class CriticalSectionSnippets
    {
        // Declaration of dummy artifacts to use in snippets
        object lk = new object();
        ITable table;
        string Expensive(string s) { return string.Empty; }

        void CriticalSectionsSize()
        {
            lock (lk) { /* do first thing */ }
            /* do second thing */
            lock (lk) { /* do third thing */ }


            lock (lk)
            {
                /* do first thing */
                /* do second thing */
                /* do third thing */
            }
        }

        void LargeCriticalSection()
        {
            int k = 1;
            string v1, v2;

            lock (lk)
            {
                v1 = table.Lookup(k);
                v2 = Expensive(v1);
                table.Remove(k);
                table.Insert(k, v2);
            }
        }

        void SplitLargeSectionWrong()
        {
            int k = 1;
            string v1, v2;

            lock (lk)
            {
                v1 = table.Lookup(k);
            }
            v2 = Expensive(v1);
            lock (lk)
            {
                table.Remove(k);
                table.Insert(k, v2);
            }
        }

        void CorrectSplitWithCaveat()
        {
            int k = 1;
            string v1, v2;

            bool loop_done = false;
            while (!loop_done)
            {
                lock (lk)
                {
                    v1 = table.Lookup(k);
                }
                v2 = Expensive(v1);
                lock (lk)
                {
                    if (table.Lookup(k) == v1)
                    {
                        loop_done = true;
                        table.Remove(k);
                        table.Insert(k, v2);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// No implementation necessary, just interface to make snippets compile 
    /// </summary>
    interface ITable
    {
        string Lookup(int key);
        void Remove(int key);
        void Insert(int key, string value);
    }
}
