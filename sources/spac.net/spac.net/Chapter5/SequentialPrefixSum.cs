namespace spac.net.Chapter5
{
    public class SequentialPrefixSum
    {
        public static int[] PrefixSum(int[] input)
        {
            int[] output = new int[input.Length];
            output[0] = input[0];
            for (int i = 1; i < input.Length; i++)
            {
                output[i] = output[i-1] + input[i];
            }

            return output;
        }
    }
}
