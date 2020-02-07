using System;
using System.Buffers;
using System.Collections.Generic;
using Rsync.Delta.Models;

namespace Rsync.Delta.Delta
{
    internal readonly struct BlockMatcher : IDisposable
    {
        public readonly SignatureOptions Options;
        private readonly LazyBlockSignature _lazyBlockSig;
        private readonly Dictionary<BlockSignature, ulong> _blocks;

        public BlockMatcher(
            SignatureOptions options,
            Dictionary<BlockSignature, ulong> signatures,
            MemoryPool<byte> memoryPool)
        {
            Options = options;
            _lazyBlockSig = new LazyBlockSignature(options, memoryPool);
            _blocks = signatures;
        }

        public void Dispose() => _lazyBlockSig.Dispose();

        public LongRange? MatchBlock(in BufferedBlock block)
        {
            _lazyBlockSig.Block = block;
            var sig = new BlockSignature(_lazyBlockSig);
            return _blocks.TryGetValue(sig, out ulong start) ?
                new LongRange(start, (ulong)block.CurrentBlock.Length) :
                (LongRange?)null;
        }
    }
}
