using NUnit.Framework;
using System.Linq;
using spac.net.Chapter3;

namespace spac.net.tests.Chapter3
{
    class DivideAndConquerTaskParallelTests
    {
        [Test]
        public void should_calculate_array_sum()
        {
            int[] arr = Enumerable.Range(0, 1000).ToArray();

            int sum = DivideAndConquerTaskParallel.Sum(arr);

            Assert.AreEqual(arr.Sum(), sum);
        }
    }
}
