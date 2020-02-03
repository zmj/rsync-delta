using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class FirstBlockTests
    {
        [Theory]
        [MemberData(nameof(MutationTest.TestCases), MemberType = typeof(MutationTest))]
        public async Task FirstBlock(BlockSequence blocks, Mutation mutation, SignatureOptions options)
        {
            using var files = new TestDirectory(nameof(FirstBlock), blocks, mutation);
            await MutationTest.Test(files, blocks, mutation, options, blockToMutate: 0);
        }
    }
}
