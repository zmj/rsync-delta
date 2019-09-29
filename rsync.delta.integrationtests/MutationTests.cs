using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class MutationTests
    {
        private readonly IRsyncAlgorithm _rsync = new RsyncAlgorithm();

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task MutateFirstBlock(BlockSequence blocks, Mutation mutation)
        {
            using var files = new TestDirectory(nameof(MutateFirstBlock), blocks, mutation);
            await blocks.WriteTo(files.Write(TestFile.v1));

            using (var v1 = files.Read(TestFile.v1))
            using (var sig = files.Write(TestFile.sig))
            {
                await _rsync.GenerateSignature(v1, sig);
            }
            // generate rdiff sig
            using (var sig = files.Read(TestFile.sig))
            using (var rssig = files.Read(TestFile.rs_sig))
            {
                await AssertEqual(rssig, sig);
            }

            var mutated = mutation.ApplyTo(blocks, index: 0);
            await mutated.WriteTo(files.Write(TestFile.v2));

            using (var sig = files.Read(TestFile.sig))
            using (var v2 = files.Read(TestFile.v2))
            using (var delta = files.Write(TestFile.delta))
            {
                await _rsync.GenerateDelta(sig, v2, delta);
            }
            // generate rdiff delta
            using (var delta = files.Read(TestFile.delta))
            using (var rsdelta = files.Read(TestFile.rs_delta))
            {
                await AssertEqual(rsdelta, delta);
            }

            // generate lib patched
            using (var patched = files.Read(TestFile.patched))
            using (var v2 = files.Read(TestFile.v2))
            {
                await AssertEqual(v2, patched);
            }
        }

        private static async Task AssertEqual(Stream expected, Stream actual)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<object[]> TestCases()
        {
            foreach (var blocks in BlockSequence.All())
            {
                foreach (var mutation in Mutation.All())
                {
                    yield return new object[] { blocks, mutation };
                }
            }
        }
    }
}