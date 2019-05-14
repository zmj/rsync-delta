using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class DeltaTests
    {
        private readonly IRsyncAlgorithm _rsync;

        public DeltaTests() => _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello_hellooo_b1")]
        public async Task Delta(string dir)
        {   
            dir = Path.GetFullPath($"../../../data/{dir}");
            byte[] expected  = await File.ReadAllBytesAsync(Path.Combine(dir, "v2.delta"));

            byte[] actual = new byte[expected.Length];
            using (var sig = File.OpenRead(Path.Combine(dir, "v1.sig")))
            using (var v2 = File.OpenRead(Path.Combine(dir, "v2.txt")))
            {
                await _rsync.GenerateDelta(sig, v2, new MemoryStream(actual));
            }
            Console.WriteLine($"actual: {BitConverter.ToString(actual)}");
            Console.WriteLine($"expected: {BitConverter.ToString(expected)}");
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }
    } 
}