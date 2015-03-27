using NUnit.Framework;
using System.Linq;
using spac.net.Chapter5;

namespace spac.net.tests.Chapter5
{
    class SequentialPrefixSumTests
    {
        [Test]
        public void should_calculate_prefix_sums()
        {
            int[] arr = Enumerable.Range(0, 1000).ToArray();
            int[] expected_output = Enumerable.Range(0, 1000)
                                              .Select(i => i * (i + 1) / 2)
                                              .ToArray();

            int[] output = SequentialPrefixSum.PrefixSum(arr);

            Assert.AreEqual(expected_output, output);
        }
    }
}
