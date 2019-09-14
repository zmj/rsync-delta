using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class SignatureTests
    {
        [Fact]
        public Task Sig_Hello_Default() => Sig(TestCase.Hello_Hellooo_Default);
/*
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Sig_Hello_BlockLength(int blockLength)
        {

        }

        [Theory]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        public async Task Sig_Hello_StrongHashLength(int strongHashLength)
        {

        }
        */

        private async Task Sig(TestCase tc)
        {
            var output = new MemoryStream();
            await new RsyncAlgorithm().GenerateSignature(
                fileStream: new MemoryStream(tc.Version1),
                signatureStream: output,
                tc.Options,
                System.Threading.CancellationToken.None);
            
            var expected = tc.Signature;
            var actual = output.ToArray();
            Assert.Equal(
                expected: BitConverter.ToString(expected.ToArray()),
                actual: BitConverter.ToString(actual));
        }
    }
}