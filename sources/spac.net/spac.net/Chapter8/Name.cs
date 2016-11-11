using System;

namespace spac.net.Chapter8
{
    class Name
    {
        public string First;
        public string Middle;
        public string Last;

        public Name(string f, string m, string l)
        {
            First = f;
            Middle = m;
            Last = l;
        }

        public override string ToString()
        {
            return First + " " + Middle + " " + Last;
        }

        private static NameTableLookup table = new NameTableLookup();

        public static string NameByIdBest(int id)
        {
            Name n = table.Lookup(id);
            return n.First + " " + n.Middle[0] + " " + n.Last;
        }

        public static string NameByIdBad(int id)
        {
            Name n = table.Lookup(id);
            n.Middle = n.Middle.Substring(0, 1);
            return n.ToString();
        }

        public static string NameByIdSingleThreadPoor(int id)
        {
            Name n = table.Lookup(id);
            String m = n.Middle;
            n.Middle = m.Substring(0, 1);
            String ans = n.ToString();
            n.Middle = m;
            return ans;
        }

        public static string NameByIdMultiThreadGood(int id)
        {
            Name n1 = table.Lookup(id);
            Name n2 = new Name(n1.First, n1.Middle.Substring(0, 1), n1.Last);
            return n2.ToString();
        }
    }

    /// <summary>
    /// Sample implementation, just enough to provide necessary interface and functionality
    /// </summary>
    class NameTableLookup
    {
        public Name Lookup(int id)
        {
            return new Name("First", "Middle", "Last");
        }
    }
}
