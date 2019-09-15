using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class DeltaTests
    {
        [Fact]
        public Task Delta_Hello_Hellooo_Default() =>
            Delta(TestCase.Hello_Hellooo_Default);

        [Fact]
        public Task Delta_Hello_Hellooo_BlockLength_1() =>
            Delta(TestCase.Hello_Hellooo_BlockLength_1);

        [Fact]
        public Task Delta_Hello_Hellooo_BlockLength_2() =>
            Delta(TestCase.Hello_Hellooo_BlockLength_2);

        [Fact]
        public Task Delta_Hello_Hellooo_StrongHashLength_16() =>
            Delta(TestCase.Hello_Hellooo_StrongHashLength_16);

        [Fact]
        public Task Delta_Hello_Hello_BlockLength_2() =>
            Delta(TestCase.Hello_Hello_BlockLength_2);

        [Fact]
        public Task Delta_Hello_Ohello_BlockLength_2() =>
            Delta(TestCase.Hello_Ohello_BlockLength_2);

        [Fact]
        public Task Delta_Hello_Ohhello_BlockLength_2() =>
            Delta(TestCase.Hello_Ohhello_BlockLength_2);

        private async Task Delta(TestCase tc)
        {
            var output = new MemoryStream();
            await new RsyncAlgorithm().GenerateDelta(
                signatureStream: new MemoryStream(tc.Signature),
                fileStream: new MemoryStream(tc.Version2),
                deltaStream: output,
                System.Threading.CancellationToken.None);

            var expected = tc.Delta;
            var actual = output.ToArray();
            Assert.Equal(
                expected: BitConverter.ToString(expected),
                actual: BitConverter.ToString(actual));
        }
    }
}