using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace spac.net.Chapter3
{
    /// <summary>
    /// This approach is theoretically right
    /// But is faulty in practice because it does not work with default .Net settings
    /// By default every thread in 32 bit app gets 1 Mb of memory in .Net
    /// And the whole app gets 2 Gb
    /// So creating about 2000 threads can easily make app run out of memory
    /// </summary>
    public class SimpleDivideAndConquerArraySum
    {
        class SumRange
        {
            int left;
            int right;
            int[] arr;
            public int Answer { get; private set; }

            public SumRange(int[] a, int l, int r)
            {
                left = l;
                right = r;
                arr = a;
                Answer = 0;
            }

            public void Run()
            {
                if (right - left == 1)
                {
                    Answer = arr[left];
                }
                else
                {
                    SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                    SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                    Thread leftThread = new Thread(leftRange.Run);
                    Thread rightThread = new Thread(rightRange.Run);
                    leftThread.Start();
                    rightThread.Start();
                    leftThread.Join();
                    rightThread.Join();

                    Answer = leftRange.Answer + rightRange.Answer;
                }
            }
        }

        public static int Sum(int[] arr)
        {
            SumRange s = new SumRange(arr, 0, arr.Length);
            s.Run();
            return s.Answer;
        }
    }
}
