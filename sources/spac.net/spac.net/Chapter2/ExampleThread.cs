using System;
using System.Threading;

namespace spac.net.Chapter2
{
    class ExampleThread
    {
        static void Run(int i)
        {
            Console.WriteLine("Thread {0} says hi", i);
            Console.WriteLine("Thread {0} says bye", i);
        }

        static void Main(string[] args)
        {
            for (int i = 1; i <= 20; i++)
            {
                int j = i;
                Thread t = new Thread(() => Run(j));
                t.Start();
            }
        }
    }
}
