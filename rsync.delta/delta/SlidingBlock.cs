using System;
using System.Buffers;

namespace Rsync.Delta.Delta
{
    internal ref struct SlidingBlock
    {
        private readonly bool _isFinalBlock;

        private Position _start;
        private Position _end;

        public SlidingBlock(
            in ReadOnlySequence<byte> sequence,
            int blockLength,
            bool isFinalBlock)
        {
            _isFinalBlock = isFinalBlock;
            _start = new Position(sequence, sequencePosition: 0);
            var endStart = Math.Min(sequence.Length, blockLength);
            _end = new Position(sequence.Slice(endStart), endStart);
        }

        private ref struct Position
        {
            private ReadOnlySequence<byte>.Enumerator _enumerator;
            private long _seqPos;
            private ReadOnlySpan<byte> _span;
            private int _spanPos;
            private byte _current;

            public Position(
                in ReadOnlySequence<byte> sequence, 
                long sequencePosition)
            {
                _enumerator = sequence.GetEnumerator();
                _seqPos = sequencePosition;
                _span = ReadOnlySpan<byte>.Empty;
                _spanPos = 0;
                _current = default;
            }

            public bool TryAdvance(
                out long sequencePosition,
                out byte previous, 
                out byte current)
            {
                while (_spanPos == _span.Length)
                {
                    _seqPos += _spanPos;
                    if (!_enumerator.MoveNext())
                    {
                        sequencePosition = _seqPos;
                        previous = default;
                        current = default;
                        return false;
                    }
                    _span = _enumerator.Current.Span;
                    _spanPos = 0;
                }
                sequencePosition = _seqPos + _spanPos;
                previous = _current;
                current = _current = _span[_spanPos];
                _spanPos++;
                return true;
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
