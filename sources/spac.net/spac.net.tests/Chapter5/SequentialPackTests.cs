using NUnit.Framework;
using System.Linq;
using spac.net.Chapter5;

namespace spac.net.tests.Chapter5
{
    class SequentialPackTests
    {
        [Test]
        public void should_output_correct_pack_array()
        {
            int[] input = new Randomizer().GetInts(0, 100, 100);
            int[] expected_output = input.Where(i => i > 10).ToArray();

            int[] output = SequentialPack.GreaterThenTen(input);

            Assert.AreEqual(expected_output, output);
        }
    }
}
