using System.IO;
using System.Text;
using System.Threading.Tasks;
using Rsync.Delta.Models;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class EndToEndTests
    {
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
        public Task EndToEnd(
            string version1, 
            string version2,
            int? blockLength = null,
            int? strongHashLength = null) =>
            E2E(
                Encoding.UTF8.GetBytes(version1),
                Encoding.UTF8.GetBytes(version2),
                new SignatureOptions(
                    blockLength ?? SignatureOptions.Default.BlockLength,
                    strongHashLength ?? SignatureOptions.Default.StrongHashLength));

        private async Task E2E(
            byte[] version1, 
            byte[] version2,
            SignatureOptions options)
        {
            IRsyncAlgorithm rsync = new RsyncAlgorithm();

            var sig = new MemoryStream();
            await rsync.GenerateSignature(
                fileStream: new MemoryStream(version1),
                signatureStream: sig,
                options);
            sig.Seek(offset: 0, SeekOrigin.Begin);

            var delta = new MemoryStream();
            await rsync.GenerateDelta(
                signatureStream: sig,
                fileStream: new MemoryStream(version2),
                deltaStream: delta);
            delta.Seek(offset: 0, SeekOrigin.Begin);

            var v2 = new MemoryStream();
            await rsync.Patch(
                deltaStream: delta,
                oldFileStream: new MemoryStream(version1),
                newFileStream: v2);

            AssertHelpers.Equal(version2, v2.ToArray());
        }
    }
}