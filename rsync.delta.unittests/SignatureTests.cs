using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class SignatureTests
    {
        [Fact]
        public Task Sig_Hello_Default() => 
            Sig(TestCase.Hello_Hellooo_Default);

        [Fact]
        public Task Sig_Hello_BlockLength_1() => 
            Sig(TestCase.Hello_Hellooo_BlockLength_1);

        [Fact]
        public Task Sig_Hello_BlockLength_2() => 
            Sig(TestCase.Hello_Hellooo_BlockLength_2);

        [Fact]
        public Task Sig_Hello_StrongHashLength_15() => 
            Sig(TestCase.Hello_Hellooo_StrongHashLength_15);

        [Fact]
        public Task Sig_Hello_StrongHashLength_16() =>
            Sig(TestCase.Hello_Hellooo_StrongHashLength_16);

        [Fact]
        public Task Sig_Hello_StrongHashLength_17() =>
            Sig(TestCase.Hello_Hellooo_StrongHashLength_17);

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
                expected: BitConverter.ToString(expected),
                actual: BitConverter.ToString(actual));
        }
    }
}