using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class LastBlockTests
    {
        [Theory]
        [MemberData(nameof(MutationTest.TestCases), MemberType = typeof(MutationTest))]
        public async Task LastBlock(BlockSequence blocks, Mutation mutation, SignatureOptions options)
        {
            using var files = new TestDirectory(nameof(LastBlock), blocks, mutation);
            await MutationTest.Test(files, blocks, mutation, options, blockToMutate: blocks.Count - 1);
        }
    }
}
