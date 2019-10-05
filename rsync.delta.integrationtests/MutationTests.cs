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
            var rdiff = new Rdiff(files);
            using (var v1 = files.Write(TestFile.v1))
            {
                await blocks.WriteTo(v1);
            }

            using (var v1 = files.Read(TestFile.v1))
            using (var sig = files.Write(TestFile.sig))
            {
                await _rsync.GenerateSignature(v1, sig);
            }
            await rdiff.Signature(TestFile.v1, TestFile.rs_sig);
            using (var sig = files.Read(TestFile.sig))
            using (var rssig = files.Read(TestFile.rs_sig))
            {
                await AssertEqual(rssig, sig);
            }

            var mutated = mutation.ApplyTo(blocks, index: 0);
            using (var v2 = files.Write(TestFile.v2))
            {
                await mutated.WriteTo(v2);
            }

            using (var sig = files.Read(TestFile.sig))
            using (var v2 = files.Read(TestFile.v2))
            using (var delta = files.Write(TestFile.delta))
            {
                await _rsync.GenerateDelta(sig, v2, delta);
            }
            await rdiff.Delta(TestFile.sig, TestFile.v2, TestFile.rs_delta);
            using (var delta = files.Read(TestFile.delta))
            using (var rsdelta = files.Read(TestFile.rs_delta))
            {
                await AssertEqual(rsdelta, delta);
            }

            using (var delta = files.Read(TestFile.delta))
            using (var v1 = files.Read(TestFile.v1))
            using (var patched = files.Write(TestFile.patched))
            {
                await _rsync.Patch(delta, v1, patched);
            }
            using (var patched = files.Read(TestFile.patched))
            using (var v2 = files.Read(TestFile.v2))
            {
                await AssertEqual(v2, patched);
            }
        }

        private static async Task AssertEqual(Stream expected, Stream actual)
        {
            const int page = 4096;
            var eBuffer = new byte[page];
            var aBuffer = new byte[page];
            for (long pos = 0; ; pos += page)
            {
                var e = await Read(expected, eBuffer.AsMemory());
                var a = await Read(actual, aBuffer.AsMemory());
                bool eq = e.Span.SequenceEqual(a.Span);
                if (!eq)
                {
                    Console.WriteLine("position: " + pos);
                    Assert.Equal(
                        expected: BitConverter.ToString(e.ToArray()),
                        actual: BitConverter.ToString(a.ToArray()));
                }
                if (e.Length != page || a.Length != page)
                {
                    break;
                }
            }

            async ValueTask<Memory<byte>> Read(Stream stream, Memory<byte> buf)
            {
                Memory<byte> toWrite = buf;
                int read;
                do
                {
                    read = await stream.ReadAsync(toWrite);
                    toWrite = toWrite.Slice(read);
                } while (toWrite.Length > 0 && read > 0);
                return buf.Slice(0, buf.Length - toWrite.Length);
            }
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