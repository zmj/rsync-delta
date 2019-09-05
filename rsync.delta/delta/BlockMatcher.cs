using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

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

        public void Add(BlockSignature sig, ulong start)
        {
#if NETSTANDARD2_0
            if (!_blocks.ContainsKey(sig))
            {
                _blocks.Add(sig, start);
            }
#else
            _blocks.TryAdd(sig, start);
#endif
        }

        public LongRange? MatchBlock(BufferedBlock block)
        {
            _lazyBlockSig.Block = block;
            var sig = new BlockSignature(_lazyBlockSig);
            return _blocks.TryGetValue(sig, out ulong start) ? 
                new LongRange(start, (ulong)block.CurrentBlock.Length) : 
                (LongRange?)null;
        }
    }
}