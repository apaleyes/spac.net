using System.Threading.Tasks;

namespace spac.net.Chapter3
{
    public class DivideAndConquerTaskParallelResult
    {
        class SumRange
        {
            static int Sequential_Cutoff = 100;
            int left;
            int right;
            int[] arr;

            public SumRange(int[] a, int l, int r)
            {
                left = l;
                right = r;
                arr = a;
            }

            public int Compute()
            {
                if (right - left < Sequential_Cutoff)
                {
                    int ans = 0;
                    for (int i = left; i < right; i++)
                    {
                        ans += i;
                    }
                    return ans;
                }
                else
                {
                    SumRange leftRange = new SumRange(arr, left, (left + right) / 2);
                    SumRange rightRange = new SumRange(arr, (left + right) / 2, right);

                    Task<int> leftTask = Task.Factory.StartNew<int>(leftRange.Compute);
                    int rightAns = rightRange.Compute();
                    leftTask.Wait();
                    int leftAns = leftTask.Result;

                    return leftAns + rightAns;
                }
            }
        }

        public static int Sum(int[] arr)
        {
            SumRange s = new SumRange(arr, 0, arr.Length);
            return s.Compute();
        }
    }
}
