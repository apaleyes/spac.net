using System.Threading;

namespace spac.net.Chapter3
{
    public class DivideAndConquerSeqCutoff
    {
        class SumRange
        {
            static int Sequential_Cutoff = 100;
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
                if (right - left < Sequential_Cutoff)
                {
                    for (int i = left; i < right; i++)
                    {
                        Answer += arr[i];
                    }
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
