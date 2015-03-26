using System.Linq;
using System.Threading;

namespace spac.net.Chapter3
{
    /// <summary>
    /// This approach is wrong
    /// because it does not wait for the in-thread calculation to finish
    /// before using its result.
    /// </summary>
    public class WrongThreadedArraySum
    {
        class SumRange
        {
            int left;
            int right;
            int[] arr;
            public int Answer { get; private set; }

            public SumRange(int[] a, int left, int right)
            {
                this.left = left;
                this.right = right;
                this.arr = a;
                Answer = 0;
            }

            public void Run()
            {
                for (int i = left; i < right; i++)
                {
                    Answer += arr[i];
                }
            }
        }

        public static int Sum(int[] arr)
        {
            int len = arr.Length;
            int ans = 0;

            SumRange[] s = new SumRange[4];
            for (int i = 0; i < 4; i++)
            {
                SumRange sr = new SumRange(arr, (i * len) / 4, ((i + 1) * len) / 4);
                s[i] = sr;
                Thread t = new Thread(() => sr.Run());
                t.Start();
            }

            for (int i = 0; i < 4; i++)
            {
                ans += s[i].Answer;
            }

            return ans;
        }
    }
}
