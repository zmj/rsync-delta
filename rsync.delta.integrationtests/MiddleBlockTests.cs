using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class MiddleBlockTests
    {
        [Theory]
        [MemberData(nameof(MutationTest.TestCases), MemberType = typeof(MutationTest))]
        public async Task MiddleBlock(BlockSequence blocks, Mutation mutation, SignatureOptions options)
        {
            using var files = new TestDirectory(nameof(MiddleBlock), blocks, mutation);
            await MutationTest.Test(files, blocks, mutation, options, blockToMutate: blocks.Count / 2);
        }
    }
}
