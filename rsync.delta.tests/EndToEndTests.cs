using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class EndToEndTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [InlineData("hello", "hellooo", 2)]
        [InlineData("hello", "hellooo", 2, 16)]
        [InlineData("hello", "hhhello", 2)]
        [InlineData("hello", "hell", 2)]
        [InlineData("hello", "hel", 2)]
        [InlineData("hello", "ello", 2)]
        [InlineData("hello", "llo", 2)]
        [InlineData("hello", "help", 2)]
        [InlineData("hello", "lo hell")]
        [InlineData("hello", "goodbye", 2)]
        [InlineData("hello", "hello there", 2)]
        [InlineData("hello", "ohello", 2)]
        [InlineData("hello", "ohhello", 2)]
        [InlineData("hello", "oh hello", 2)]
        [InlineData("hello", "oh hello there", 2)]
        public async Task Test(string v1, string v2, int? blockLength = null, int? strongHashLength = null)
        {
            string actual = await Do(v1, v2, Options(blockLength, strongHashLength));
            Assert.Equal(v2, actual);
        }

        private async Task<string> Do(string v1, string v2, SignatureOptions options)
        {
            var utf8 = Encoding.UTF8;
            return utf8.GetString(await Do(utf8.GetBytes(v1), utf8.GetBytes(v2), options));
        }

        private async Task<byte[]> Do(byte[] v1, byte[] v2, SignatureOptions options)
        {
            var signature = new MemoryStream();
            await _rsync.GenerateSignature(
                new MemoryStream(v1),
                signature,
                options);

            var delta = new MemoryStream();
            await _rsync.GenerateDelta(
                new MemoryStream(signature.ToArray()),
                new MemoryStream(v2),
                delta);

            var patched = new MemoryStream();
            await _rsync.Patch(
                new MemoryStream(delta.ToArray()),
                new MemoryStream(v1),
                patched);
            return patched.ToArray();
        }

        private SignatureOptions Options(int? blockLength, int? strongHashLength) =>
            new SignatureOptions(
                blockLength ?? SignatureOptions.Default.BlockLength,
                strongHashLength ?? SignatureOptions.Default.StrongHashLength);
    }
}