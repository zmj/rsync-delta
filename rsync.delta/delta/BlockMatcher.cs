using System;
using System.Buffers;
using System.Collections.Generic;
using Rsync.Delta.Hash;
using Rsync.Delta.Models;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Delta
{
    internal sealed class BlockMatcher : IDisposable
    {
        public readonly SignatureOptions Options;
        private readonly Dictionary<BlockSignature, ulong> _blocks;
        private readonly int _blockLength;
        private readonly IRollingHashAlgorithm _rollingHash;
        private readonly IStrongHashAlgorithm _strongHash;
        private readonly IMemoryOwner<byte> _strongHashBuffer;

        private ReadOnlySequence<byte> _sequence;
        private bool _recalculateStrongHash;

        public BlockMatcher(
            SignatureOptions options,
            Dictionary<BlockSignature, ulong> signatures,
            MemoryPool<byte> memoryPool)
        {
            Options = options;
            _blockLength = options.BlockLength;
            _rollingHash = HashAlgorithmFactory.Create(options.RollingHash);
            _strongHash = HashAlgorithmFactory.Create(options.StrongHash, memoryPool);
            _strongHashBuffer = memoryPool.Rent(options.StrongHashLength);
            _blocks = signatures;
        }

        public OperationStatus MatchBlock(
            in ReadOnlySequence<byte> sequence,
            bool isFinalBlock,
            out LongRange? match,
            out long consumed)
        {
            _sequence = sequence;
            // inner loop:
            // sliding block byte-by-byte
            // return NeedMoreData if !isFinalBlock and end of buffer (otherwise shrink)
            // dict.tryget(new BlockSig(rollingHash, this))
            // true: matched, return a Done + longrange
            //  * what about a literal before the match?
            // false: coninue
            // max literal len: return Done + match:null
            var block = new SlidingBlock(sequence, Options.BlockLength, isFinalBlock);
            while (block.TryAdvance(
                out long start, out long length,
                out byte removed, out byte added))
            {
                if (isFinalBlock && length < _blockLength)
                {
                    _rollingHash.RotateOut(removed);
                }
                else
                {
                    _rollingHash.Rotate(removed, added);
                }
                _recalculateStrongHash = true;
                var sig = new BlockSignature(_rollingHash.Value, this, start, length);
                if (_blocks.TryGetValue(sig, out ulong matchStart))
                {
                    throw new NotImplementedException();
                }

                // todo: need an examined start arg to count up from for maxLiteralLength
                // alternate: check that outside of this loop? 
                // either after the break or in deltaWriter?
                // tentative: in deltaWriter, after this call, before Advance
                // this allows a pre-sliced ROS to be passed into here
            }
            match = null;
            consumed = 0;
            return OperationStatus.NeedMoreData;
        }

        public ReadOnlyMemory<byte> GetStrongHash(long start, long length)
        {
            var strongHash = _strongHashBuffer.Memory;
            if (_recalculateStrongHash)
            {
                _strongHash.Hash(
                    _sequence.Slice(start, length),
                    strongHash.Span);
                _recalculateStrongHash = false;
            }
            return strongHash;
        }

        /*public LongRange? MatchBlock(in BufferedBlock block)
        {
            BufferedBlock = block;
            var sig = new BlockSignature(this);
            return _blocks.TryGetValue(sig, out ulong start) ?
                new LongRange(start, (ulong)block.CurrentBlock.Length) :
                (LongRange?)null;
        }

        public int RollingHash => _rollingHash.Value;

        public ReadOnlyMemory<byte> StrongHash
        {
            get
            {
                if (_recalculateStrongHash)
                {
                    _strongHash.Hash(
                        _block.CurrentBlock,
                        _strongHashBuffer.Memory.Span);
                    _recalculateStrongHash = false;
                }
                return _strongHashBuffer.Memory;
            }
        }

        private BufferedBlock BufferedBlock
        {
            get => _block;
            set
            {
                _block = value;
                UpdateRollingHash();
                _recalculateStrongHash = true;
            }
        }

        private void UpdateRollingHash()
        {
            if (_block.PendingLiteral.IsEmpty)
            {
                _rollingHash.Initialize(_block.CurrentBlock);
            }
            else if (_block.CurrentBlock.Length == Options.BlockLength)
            {
                _rollingHash.Rotate(
                    remove: _block.PendingLiteral.LastByte(),
                    add: _block.CurrentBlock.LastByte());
            }
            else
            {
                _rollingHash.RotateOut(_block.PendingLiteral.LastByte());
            }
        }*/

        public void Dispose()
        {
            _strongHashBuffer.Dispose();
            _strongHash.Dispose();
        }
    }
}
