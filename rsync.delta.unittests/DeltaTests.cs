using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class DeltaTests
    {
        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Hellooo_Default(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Hellooo_Default, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Hellooo_BlockLength_1(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Hellooo_BlockLength_1, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Hellooo_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Hellooo_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Hellooo_StrongHashLength_16(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Hellooo_StrongHashLength_16, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Hello_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Hello_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Ohello_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Ohello_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Ohhello_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Ohhello_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_Hello_Heollo_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.Hello_Heollo_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType = typeof(TestCase))]
        public Task Delta_LoremIpsum(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Delta(TestCase.LoremIpsum, rollingHash, strongHash);

        private async Task Delta(
            TestCase tc,
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash)
        {
            var output = new MemoryStream();
            var signature = tc.Sig(rollingHash, strongHash);
            await new Rdiff().DeltaAsync(
                signature: new MemoryStream(signature),
                newFile: new MemoryStream(tc.Version2),
                delta: output,
                System.Threading.CancellationToken.None);

            var expected = tc.Delta;
            var actual = output.ToArray();
            AssertHelpers.Equal(expected, actual);
        }
    }
}
