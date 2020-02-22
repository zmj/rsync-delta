using System;
using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal ref struct SlidingBlock
    {
        private readonly int _blockLength;
        private readonly bool _isFinalBlock;

        private Position _start;
        private Position _end;

        public SlidingBlock(
            in ReadOnlySequence<byte> sequence,
            int blockLength,
            bool isFinalBlock)
        {
            throw new NotImplementedException();
        }

        private ref struct Position
        {
            private ReadOnlySequence<byte>.Enumerator _enumerator;
            private long _seqPos;
            private ReadOnlySpan<byte> _span;
            private int _spanPos;

            public bool TryAdvance(
                out long sequencePosition,
                out byte previous, 
                out byte current)
            {
                // two-tier enumeration
                // enumerate within current spans (inner loop)
                // else advance to next span
                throw new NotImplementedException();
            }
        }

        public bool TryAdvance(
            out long start, out long length, 
            out byte removed, out byte added)
        {
            if ((_end.TryAdvance(out long end, out _, out added) || _isFinalBlock) &&
                _start.TryAdvance(out start, out removed, out _))
            {
                length = end - start;
                return true;
            }
            start = default;
            length = default;
            removed = default;
            return false;
        }
    }
}