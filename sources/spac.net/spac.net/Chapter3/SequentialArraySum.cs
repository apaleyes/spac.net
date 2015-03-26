using System.Linq;

namespace spac.net.Chapter3
{
    public class SequentialArraySum
    {
        public static int Sum(int[] arr)
        {
            int ans = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                ans += arr[i];
            }

            return ans;
        }
    }
}
