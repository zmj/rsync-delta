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
            using TempFile sig = await Rdiff.Signature(
                new MemoryStream(Encoding.UTF8.GetBytes(v1)), 
                blockLength, strongHashLength);
            
            var v2Bytes = Encoding.UTF8.GetBytes(v2);
            using TempFile rdiffOut = await Rdiff.Delta(sig, new MemoryStream(v2Bytes));
            var expected = BitConverter.ToString(await rdiffOut.Bytes());

            var libraryOut = new MemoryStream();
            await _rsync.GenerateDelta(sig.Stream(), new MemoryStream(v2Bytes), libraryOut);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}