using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class SignatureTests
    {
        [Theory]
        [InlineData("hello_hellooo")]
        [InlineData("hello_hellooo_b1")]
        [InlineData("hello_hellooo_b2")]
        [InlineData("hello_b2")]
        public async Task Signature(string dir)
        {
            dir = Path.GetFullPath($"../../../data/{dir}");
            byte[] expected  = await File.ReadAllBytesAsync(Path.Combine(dir, "v1.sig"));
            bool specifyBlockSize = int.TryParse(dir[^1..], out int blockSize);

            byte[] actual = new byte[expected.Length];
            PipeWriter writer = PipeWriter.Create(new MemoryStream(actual));
            using (var f = File.OpenRead(Path.Combine(dir, "v1.txt")))
            {
                PipeReader reader = PipeReader.Create(f);
                await new Signature().Generate(reader, writer, specifyBlockSize ? blockSize : (int?)null);
            }
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }
    }
}
