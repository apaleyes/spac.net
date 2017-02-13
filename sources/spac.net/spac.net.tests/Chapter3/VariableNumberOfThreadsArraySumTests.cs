using NUnit.Framework;
using System.Linq;
using spac.net.Chapter3;

namespace spac.net.tests.Chapter3
{
    class VariableNumberOfThreadsArraySumTests
    {
        [TestFixture]
        class CorrectThreadedArraySumTests
        {
            [Test]
            public void should_calculate_array_sum()
            {
                int[] arr = Enumerable.Range(0, 1000).ToArray();

                int sum = VariableNumberOfThreadsArraySum.Sum(arr, 3);

                Assert.AreEqual(arr.Sum(), sum);
            }
        }
    }
}
