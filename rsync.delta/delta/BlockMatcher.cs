using System;
using System.Buffers;
using System.Collections.Generic;
using Rsync.Delta.Hash.Adler;
using Rsync.Delta.Hash.Blake2b;
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
            MemoryPool<byte> memoryPool)
        {
            Options = options;
            _lazyBlockSig = new LazyBlockSignature(options, memoryPool);
            _blocks = new Dictionary<BlockSignature, ulong>();
        }

        public void Dispose() => _lazyBlockSig.Dispose();

        public void Add(in BlockSignature sig, ulong start)
        {
#if !NETSTANDARD2_0
            _blocks.TryAdd(sig, start);
#else
            if (!_blocks.ContainsKey(sig))
            {
                _blocks.Add(sig, start);
            }
#endif
        }

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
