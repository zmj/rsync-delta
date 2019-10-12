using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rsync.Delta;
using Rsync.Delta.IntegrationTests;

namespace Rsync.Delta.Benchmarks
{
    class TestCase
    {
        private readonly BlockSequence _blockSeq;
        private readonly Mutation _mutation;
        private readonly Func<int, bool> _shouldMutate;
        private readonly IRsyncAlgorithm _rsync;

        public TestCase(BlockSequence blockSeq, Mutation mutation, Func<int, bool> shouldMutate)
        {
            _blockSeq = blockSeq;
            _mutation = mutation;
            _shouldMutate = shouldMutate;
            _rsync = new RsyncAlgorithm();
        }

        public Stream V1() => _blockSeq.Blocks.AsStream();

        public Stream V2() => _mutation.ApplyTo(_blockSeq.Blocks, _shouldMutate).AsStream();

        public async Task<Stream> Signature(Stream v1)
        {
            v1.Seek(offset: 0, SeekOrigin.Begin);
            var sig = new MemoryStream();
            await _rsync.GenerateSignature(v1, sig);
            return sig;
        }

        public async Task<Stream> Delta(Stream sig, Stream v2)
        {
            sig.Seek(offset: 0, SeekOrigin.Begin);
            v2.Seek(offset: 0, SeekOrigin.Begin);
            var delta = new MemoryStream();
            await _rsync.GenerateDelta(sig, v2, delta);
            delta.Seek(offset: 0, SeekOrigin.Begin);
            return delta;
        }

        public async Task<Stream> Patch(Stream delta, Stream v1)
        {
            delta.Seek(offset: 0, SeekOrigin.Begin);
            v1.Seek(offset: 0, SeekOrigin.Begin);
            var patched = new MemoryStream();
            await _rsync.Patch(delta, v1, patched);
            return patched;
        }
    }

    static class Extensions
    {
        public static Stream AsStream(this IEnumerable<byte[]> blocks)
        {
            var stream = new MemoryStream();
            foreach (var block in blocks)
            {
                stream.Write(block.AsSpan());
            }
            stream.Seek(offset: 0, SeekOrigin.Begin);
            return stream;
        }
    }
}
