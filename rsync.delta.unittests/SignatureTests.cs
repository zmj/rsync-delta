using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class SignatureTests
    {
        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_Default(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_Default, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_BlockLength_1(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_BlockLength_1, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_BlockLength_2(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_BlockLength_2, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_StrongHashLength_15(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_StrongHashLength_15, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_StrongHashLength_16(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_StrongHashLength_16, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_Hello_StrongHashLength_17(
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash) =>
            Sig(TestCase.Hello_Hellooo_StrongHashLength_17, rollingHash, strongHash);

        [Theory]
        [MemberData(nameof(TestCase.HashAlgorithms), MemberType=typeof(TestCase))]
        public Task Sig_LoremIpsum(
            RollingHashAlgorithm rollingHash, 
            StrongHashAlgorithm strongHash) => 
            Sig(TestCase.LoremIpsum, rollingHash, strongHash);

        private async Task Sig(
            TestCase tc, 
            RollingHashAlgorithm rollingHash,
            StrongHashAlgorithm strongHash)
        {
            var output = new MemoryStream();
            await new Rdiff().Signature(
                oldFile: new MemoryStream(tc.Version1),
                signature: output,
                new SignatureOptions(
                    blockLength: tc.BlockLength,
                    strongHashLength: tc.StrongHashLength),
                System.Threading.CancellationToken.None);

            var expected = tc.Sig(rollingHash, strongHash);
            var actual = output.ToArray();
            AssertHelpers.Equal(expected, actual);
        }
    }
}
