using System.Diagnostics;

namespace spac.net.Chapter7
{
    class VolatileNoDataRaces
    {
        private volatile int x = 0;
        private volatile int y = 0;

        void F()
        {
            x = 1; // line A
            y = 1; // line B
        }

        void G()
        {
            int a = x; // line C
            int b = y; // line D
            Debug.Assert(b >= a);
        }
    }
}
