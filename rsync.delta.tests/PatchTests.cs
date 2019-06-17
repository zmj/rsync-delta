using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class PatchTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello_hellooo")]
        [InlineData("hello_hellooo_b1")]
        [InlineData("hello_hellooo_b2")]
        [InlineData("hello_b2")]
        [InlineData("hello_hellooo_s16")]
        public async Task Patch(string dir)
        {   
            dir = Path.GetFullPath($"../../../data/{dir}");
            byte[] expected  = await File.ReadAllBytesAsync(Path.Combine(dir, "v1.patched"));

            byte[] actual = new byte[expected.Length];
            using (var delta = File.OpenRead(Path.Combine(dir, "v2.delta")))
            using (var v1 = File.OpenRead(Path.Combine(dir, "v1.txt")))
            {
                await _rsync.Patch(delta, v1, new MemoryStream(actual));
            }
            // Console.WriteLine($"expected: {BitConverter.ToString(expected)}");
            // Console.WriteLine($"actual: {BitConverter.ToString(actual)}");
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }
    }
}