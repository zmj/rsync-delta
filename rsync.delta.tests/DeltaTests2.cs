using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class DeltaTests2
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", "hellooo", null, null)]
        [InlineData("hello", "hellooo", 1, null)]
        [InlineData("hello", "hellooo", 2, null)]
        [InlineData("hello", "hellooo", null, 16)]
        [InlineData("hello", "hello", 2, null)]
        public async Task Delta(
            string v1, string v2, int? blockLength, int? strongHashLength)
        {
            using var sig = new TempFile();
            await Rdiff
                .Signature(blockLength, strongHashLength)
                .Execute(new MemoryStream(Encoding.UTF8.GetBytes(v1)), sig.Stream);
            
            var v2Bytes = Encoding.UTF8.GetBytes(v2);
            var rdiffOut = new MemoryStream();
            await Rdiff.Delta(sig).Execute(new MemoryStream(v2Bytes), rdiffOut);
            var expected = BitConverter.ToString(rdiffOut.ToArray());

            var libraryOut = new MemoryStream();
            await _rsync.GenerateDelta(sig.Stream, new MemoryStream(v2Bytes), libraryOut);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}