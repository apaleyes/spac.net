using System.Diagnostics;

namespace spac.net.Chapter7
{
    class LockNoDataRaces
    {
        private int x = 0;
        private int y = 0;

        void F()
        {
            lock (this) { x = 1; } // line A
            lock (this) { y = 1; } // line B
        }

        void G()
        {
            int a, b;
            lock (this) { a = x; } // line C
            lock (this) { b = y; } // line D
            Debug.Assert(b >= a);
        }
    }
}
