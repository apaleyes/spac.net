using System.Diagnostics;

namespace spac.net.Chapter7
{
    class ClassWithDataRaces
    {
        private int x = 0;
        private int y = 0;

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
