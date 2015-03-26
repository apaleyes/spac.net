using NUnit.Framework;
using System.Linq;
using spac.net.Chapter3;

namespace spac.net.tests.Chapter3
{
    class DivideAndConquerSeqCutoffTests
    {
        [Test]
        public void should_calculate_array_sum()
        {
            int[] arr = System.Linq.Enumerable.Range(0, 1000).ToArray();

            int sum = DivideAndConquerSeqCutoff.Sum(arr);

            Assert.AreEqual(arr.Sum(), sum);
        }
    }
}
