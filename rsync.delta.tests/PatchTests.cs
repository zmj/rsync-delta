using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class PatchTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", "hellooo", null, null)]
        [InlineData("hello", "hellooo", 1, null)]
        [InlineData("hello", "hellooo", 2, null)]
        [InlineData("hello", "hello", 2, null)]
        [InlineData("hello", "hellooo", null, 16)]
        [InlineData("hello", "ohello", 2, null)]
        [InlineData("hello", "ohhello", 2, null)]
        public async Task Patch(
            string v1, string v2, int? blockLength, int? strongHashLength)
        {
            using TempFile sig = await Rdiff.Signature(
                new MemoryStream(Encoding.UTF8.GetBytes(v1)),
                blockLength, strongHashLength);
            using TempFile delta = await Rdiff.Delta(
                sig, new MemoryStream(Encoding.UTF8.GetBytes(v2)));

            using TempFile basis = new TempFile();
            using (var basisStream = basis.Stream())
                await new MemoryStream(Encoding.UTF8.GetBytes(v1)).CopyToAsync(basisStream);
            using TempFile rdiffOut = await Rdiff.Patch(basis, delta);
            var expected = BitConverter.ToString(await rdiffOut.Bytes());

            var libraryOut = new MemoryStream();
            using (var deltaStream = delta.Stream())
                await _rsync.Patch(
                    deltaStream, 
                    new MemoryStream(Encoding.UTF8.GetBytes(v1)), 
                    libraryOut);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}