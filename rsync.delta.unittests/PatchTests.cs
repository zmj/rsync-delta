using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.UnitTests
{
    public class PatchTests
    {
        [Fact]
        public Task Patch_Hello_Hellooo_Default() =>
            Patch(TestCase.Hello_Hellooo_Default);

        [Fact]
        public Task Hello_Hellooo_BlockLength_1() =>
            Patch(TestCase.Hello_Hellooo_BlockLength_1);

        [Fact]
        public Task Hello_Hellooo_BlockLength_2() =>
            Patch(TestCase.Hello_Hellooo_BlockLength_2);

        [Fact]
        public Task Hello_Hello_BlockLength_2() =>
            Patch(TestCase.Hello_Hello_BlockLength_2);

        [Fact]
        public Task Hello_Hellooo_StrongHashLength_16() =>
            Patch(TestCase.Hello_Hellooo_StrongHashLength_16);

        [Fact]
        public Task Hello_Ohello_BlockLength_2() =>
            Patch(TestCase.Hello_Ohello_BlockLength_2);

        [Fact]
        public Task Hello_Ohhello_BlockLength_2() =>
            Patch(TestCase.Hello_Ohhello_BlockLength_2);

        private async Task Patch(TestCase tc)
        {
            var output = new MemoryStream();
            await new Rdiff().Patch(
                oldFile: new MemoryStream(tc.Version1),
                delta: new MemoryStream(tc.Delta),
                newFile: output,
                System.Threading.CancellationToken.None);

            var expected = tc.Version2;
            var actual = output.ToArray();
            AssertHelpers.Equal(expected, actual);
        }
    }
}
