namespace spac.net.Chapter5
{
    public class SequentialPack
    {
        public static int[] GreaterThenTen(int[] input)
        {
            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 10)
                    count++;
            }

            int[] output = new int[count];
            int index = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 10)
                {
                    output[index] = input[i];
                    index++;
                }
            }

            return output;
        }
    }
}
