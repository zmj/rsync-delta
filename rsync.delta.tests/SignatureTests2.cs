using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class SignatureTests2
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", null, null)]
        [InlineData("hello", 1, null)]
        [InlineData("hello", 2, null)]
        // [InlineData("hello_hellooo_s16")] TODO: why doesn't this work?
        public async Task Signature(
            string text, int? blockLength, int? strongHashLength)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var rdiffOut = new MemoryStream();
            var rdiff = Rdiff.Signature(blockLength, strongHashLength);
            await rdiff.Execute(new MemoryStream(bytes), rdiffOut);
            var expected = BitConverter.ToString(rdiffOut.ToArray());

            var options = new SignatureOptions(
                (uint?)blockLength ?? SignatureOptions.Default.BlockLength,
                (uint?)strongHashLength ?? SignatureOptions.Default.StrongHashLength);
            var libraryOut = new MemoryStream();
            await _rsync.GenerateSignature(new MemoryStream(bytes), libraryOut, options);
            var actual = BitConverter.ToString(libraryOut.ToArray());

            Assert.Equal(expected, actual);
        }
    }
}