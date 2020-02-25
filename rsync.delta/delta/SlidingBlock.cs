using System;
using System.Buffers;
using System.Diagnostics;
using Rsync.Delta.Hash;

namespace Rsync.Delta.Delta
{
    internal ref struct SlidingBlock
    {
        private readonly IRollingHashAlgorithm _rollingHash;
        private readonly int _blockLength;
        private readonly bool _isFinalBlock;

        private State _state;
        private SequenceIndex _start;
        private SequenceIndex _end;

        public SlidingBlock(
            in ReadOnlySequence<byte> sequence,
            int blockLength,
            bool isFinalBlock,
            IRollingHashAlgorithm rollingHash)
        {
            Debug.Assert(blockLength > 0);
            _rollingHash = rollingHash;
            _blockLength = blockLength;
            _isFinalBlock = isFinalBlock;
            _state = State.Uninitialized;
            _start = new SequenceIndex(sequence);
            _end = new SequenceIndex(sequence);
        }

        private enum State
        {
            Uninitialized = 0,
            AdvancingStartAndEnd,
            AdvancingStart,
            Done,
        }

        public bool TryAdvance(
            out long start, 
            out long length, 
            out int rollingHash)
        {
            switch (_state)
            {
                case State.Uninitialized:
                    return TryInitialize(out start, out length, out rollingHash);
                case State.AdvancingStartAndEnd:
                    return TryAdvanceStartAndEnd(out start, out length, out rollingHash);
                case State.AdvancingStart:
                    return TryAdvanceStart(out start, out length, out rollingHash);
                default:
                    Debug.Assert(_state == State.Done);
                    start = default;
                    length = default;
                    rollingHash = default;
                    return false;
            }
        }

        private bool TryInitialize(
            out long start, 
            out long length, 
            out int rollingHash)
        {
            Debug.Assert(_state == State.Uninitialized);
            _rollingHash.Reset();
            if (_isFinalBlock)
            {
                return TryInitializeFinalBlock(out start, out length, out rollingHash);
            }

            rollingHash = default;
            for (int i = 0; i < _blockLength; i++)
            {
                if (_end.TryAdvance(out _, out _, out byte added))
                {
                    rollingHash = _rollingHash.RotateIn(added);
                }
                else
                {
                    _state = State.Done;
                    start = default;
                    length = default;
                    rollingHash = default;
                    return false;
                }
            }
            bool ok = _start.TryAdvance(out _, out _, out _);
            Debug.Assert(ok);

            _state = State.AdvancingStartAndEnd;
            start = 0;
            length = _blockLength;
            return true;
        }

        private bool TryInitializeFinalBlock(
            out long start,
            out long length,
            out int rollingHash)
        {
            Debug.Assert(_state == State.Uninitialized);
            Debug.Assert(_isFinalBlock);
            if (_end.TryAdvance(out _, out _, out byte added))
            {
                rollingHash = _rollingHash.RotateIn(added);
            }
            else
            {
                _state = State.Done;
                start = default;
                length = default;
                rollingHash = default;
                return false;
            }
            bool ok = _start.TryAdvance(out _, out _, out _);
            Debug.Assert(ok);

            start = 0;
            for (int i = 1; i < _blockLength; i++)
            {
                if (_end.TryAdvance(out _, out _, out added))
                {
                    rollingHash = _rollingHash.RotateIn(added);
                }
                else
                {
                    _state = State.AdvancingStart;
                    length = i;
                    return true;
                }
            }
            _state = State.AdvancingStartAndEnd;
            length = _blockLength;
            return true;
        }

        private bool TryAdvanceStartAndEnd(
            out long start, 
            out long length, 
            out int rollingHash)
        {
            Debug.Assert(_state == State.AdvancingStartAndEnd);
            if (_end.TryAdvance(out _, out _, out byte added) &&
                _start.TryAdvance(out start, out byte removed, out _))
            {
                length = _blockLength;
                rollingHash = _rollingHash.Rotate(removed, added);
                return true;
            }
            else if (_isFinalBlock)
            {
                _state = State.AdvancingStart;
                return TryAdvanceStart(out start, out length, out rollingHash);
            }
            _state = State.Done;
            start = default;
            length = default;
            rollingHash = default;
            return false;
        }

        private bool TryAdvanceStart(
            out long start, 
            out long length, 
            out int rollingHash)
        {
            Debug.Assert(_state == State.AdvancingStart);
            Debug.Assert(_isFinalBlock);
            if (_start.TryAdvance(out start, out byte removed, out _))
            {
                bool ok = _end.TryAdvance(out var end, out _, out _);
                Debug.Assert(!ok);
                length = end - start;
                rollingHash = _rollingHash.RotateOut(removed);
                return true;
            }
            _state = State.Done;
            start = default;
            length = default;
            rollingHash = default;
            return false;
        }
    }
}
