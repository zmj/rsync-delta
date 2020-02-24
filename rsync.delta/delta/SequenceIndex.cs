using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Rsync.Delta.Delta
{
    internal ref struct SequenceIndex
    {
        private ReadOnlySequence<byte>.Enumerator _enumerator;
        private long _seqPos;
        private ReadOnlySpan<byte> _span;
        private int _spanPos;
        private byte _current;

        public SequenceIndex(in ReadOnlySequence<byte> sequence)
        {
            _enumerator = sequence.GetEnumerator();
            _seqPos = 0;
            _span = ReadOnlySpan<byte>.Empty;
            _spanPos = 0;
            _current = default;
        }

        public bool TryAdvance(
            out long sequencePosition,
            out byte previous,
            out byte current)
        {
            // put fast path first
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
}
