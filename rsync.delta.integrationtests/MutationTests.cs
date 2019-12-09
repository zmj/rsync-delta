using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.IntegrationTests
{
    public class MutationTests
    {
        private readonly IRdiff _rdiff = new Rsync.Delta.Rdiff();

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task FirstBlock(BlockSequence blocks, Mutation mutation)
        {
            using var files = new TestDirectory(nameof(FirstBlock), blocks, mutation);
            await Test(files, blocks, mutation, blockToMutate: 0);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task AllBlocks(BlockSequence blocks, Mutation mutation)
        {
            using var files = new TestDirectory(nameof(AllBlocks), blocks, mutation);
            await Test(files, blocks, mutation);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task LastBlock(BlockSequence blocks, Mutation mutation)
        {
            using var files = new TestDirectory(nameof(LastBlock), blocks, mutation);
            await Test(files, blocks, mutation, blockToMutate: blocks.Count - 1);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task MiddleBlock(BlockSequence blocks, Mutation mutation)
        {
            using var files = new TestDirectory(nameof(MiddleBlock), blocks, mutation);
            await Test(files, blocks, mutation, blockToMutate: blocks.Count / 2);
        }

        private async Task Test(
            TestDirectory files,
            BlockSequence blockSequence,
            Mutation mutation,
            int? blockToMutate = null,
            SignatureOptions? options = null)
        {
            var rdiff = new Rdiff(files);
            var timings = new List<(TestFile, TimeSpan)>();
            var timer = Stopwatch.StartNew();

            blockSequence.WriteTo(files.Path(TestFile.v1));

            timings.Add((TestFile.v1, timer.Elapsed));
            timer.Restart();

            using (var v1 = files.Read(TestFile.v1))
            using (var sig = files.Write(TestFile.sig))
            {
                await _rdiff.Signature(v1, sig);
            }

            timings.Add((TestFile.sig, timer.Elapsed));
            timer.Restart();

            rdiff.Signature(TestFile.v1, TestFile.rs_sig);

            timings.Add((TestFile.rs_sig, timer.Elapsed));
            await AssertEqual(files, TestFile.rs_sig, TestFile.sig);
            timer.Restart();

            using (var v2 = files.Write(TestFile.v2))
            using (var v1 = files.Read(TestFile.v1))
            {
                await mutation.WriteTo(
                    blockSequence.Blocks(v1),
                    i => i == (blockToMutate ?? i),
                    v2);
            }

            timings.Add((TestFile.v2, timer.Elapsed));
            timer.Restart();

            using (var sig = files.Read(TestFile.sig))
            using (var v2 = files.Read(TestFile.v2))
            using (var delta = files.Write(TestFile.delta))
            {
                await _rdiff.Delta(sig, v2, delta);
            }

            timings.Add((TestFile.delta, timer.Elapsed));
            timer.Restart();

            rdiff.Delta(TestFile.sig, TestFile.v2, TestFile.rs_delta);

            timings.Add((TestFile.rs_delta, timer.Elapsed));
            await AssertEqual(files, TestFile.rs_delta, TestFile.delta);
            timer.Restart();

            using (var delta = files.Read(TestFile.delta))
            using (var v1 = files.Read(TestFile.v1))
            using (var patched = files.Write(TestFile.patched))
            {
                await _rdiff.Patch(v1, delta, patched);
            }

            timings.Add((TestFile.patched, timer.Elapsed));
            await AssertEqual(files, TestFile.v2, TestFile.patched);

            foreach (var (file, duration) in timings)
            {
                // Console.WriteLine($"{file}: {duration}");
            }
        }

        private static async Task AssertEqual(
            TestDirectory files,
            TestFile expected,
            TestFile actual)
        {
            const int page = 4096;
            var eBuffer = new byte[page];
            var aBuffer = new byte[page];
            using var eStream = files.Read(expected);
            using var aStream = files.Read(actual);
            for (long offset = 0; ; offset += page)
            {
                var e = await Read(eStream, eBuffer.AsMemory());
                var a = await Read(aStream, aBuffer.AsMemory());
                bool eq = e.Span.SequenceEqual(a.Span);
                if (!eq)
                {
                    Console.WriteLine($"expected:{expected} actual:{actual} offset:{offset}");
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
