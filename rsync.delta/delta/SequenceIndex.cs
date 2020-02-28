using System;
using System.Buffers;
using System.Diagnostics;

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
            out long position,
            out byte previous,
            out byte current)
        {
            if (_spanPos < _span.Length)
            {
                position = _seqPos + _spanPos;
                previous = _current;
                current = _current = _span[_spanPos];
                _spanPos++;
                return true;
            }
            return TryAdvanceSpan(out position, out previous, out current);
        }

        private bool TryAdvanceSpan(
            out long position,
            out byte previous,
            out byte current)
        {
            Debug.Assert(_spanPos == _span.Length);
            do
            {
                _seqPos += _span.Length;
                _span = ReadOnlySpan<byte>.Empty;
                _spanPos = 0;
                if (!_enumerator.MoveNext())
                {
                    position = _seqPos;
                    previous = default;
                    current = default;
                    return false;
                }
                _span = _enumerator.Current.Span;
            } while (_span.IsEmpty);
            return TryAdvance(out position, out previous, out current);
        }
    }
}
