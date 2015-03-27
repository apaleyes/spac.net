using NUnit.Framework;
using System.Linq;
using spac.net.Chapter3;

namespace spac.net.tests.Chapter3
{
    [TestFixture]
    class WrongThreadedArraySumTests
    {
        [Test]
        // This test may occasionally fail, see class comment for the reason why
        public void should_not_calculate_array_sum()
        {
            int[] arr = Enumerable.Range(0, 1000).ToArray();

            int sum = WrongThreadedArraySum.Sum(arr);

            Assert.AreNotEqual(arr.Sum(), sum);
        }
    }
}
