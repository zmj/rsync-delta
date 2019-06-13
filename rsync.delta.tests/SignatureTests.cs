using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class SignatureTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        /*[InlineData("hello_hellooo")]
        [InlineData("hello_hellooo_b1")]
        [InlineData("hello_hellooo_b2")]
        [InlineData("hello_b2")]*/
        [InlineData("hello_hellooo_s16")]
        public async Task Signature(string dir)
        {
            dir = Path.GetFullPath($"../../../data/{dir}");
            byte[] expected  = await File.ReadAllBytesAsync(Path.Combine(dir, "v1.sig"));
            int blockSize = 0;
            bool specifyBlockSize = dir[^2] == 'b' &&
                int.TryParse(dir[^1..], out blockSize);
            int strongHashSize = 0;
            bool specifyStrongHash = dir[^3] == 's' &&
                int.TryParse(dir[^2..], out strongHashSize);

            byte[] actual = new byte[expected.Length];
            using (var f = File.OpenRead(Path.Combine(dir, "v1.txt")))
            {
                var options = new SignatureOptions(
                    specifyBlockSize ? (uint)blockSize : 2048,
                    specifyStrongHash ? (uint)strongHashSize : 32);
                await _rsync.GenerateSignature(f, new MemoryStream(actual), options);
            }
            Console.WriteLine($"expected: {BitConverter.ToString(expected)}");
            Console.WriteLine($"actual: {BitConverter.ToString(actual)}");
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }
    }
}
