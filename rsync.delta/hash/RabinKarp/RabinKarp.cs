using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash.RabinKarp
{
    internal sealed class RabinKarp : IRollingHashAlgorithm
    {
        private const int _magic = 0x08104225;
        private const int _inverseMagic = unchecked((int)0x98F009AD);
        private const int _adjustment = 0x08104224;

        public int Value { get; private set; }

        private int _multiplier;

        public RabinKarp() => Reset();

        public void Reset()
        {
            Value = 1;
            _multiplier = 1;
        }

        public int Rotate(byte remove, byte add)
        {
            Value = 
                Value * _magic +
                add -
                _multiplier * (remove + _adjustment);
            return Value;
        }

        public int RotateIn(byte add)
        {
            _multiplier *= _magic;
            Value = Value * _magic + add;
            return Value;
        }

        public int RotateOut(byte remove)
        {
            _multiplier *= _inverseMagic;
            Value -= _multiplier * (remove + _adjustment);
            return Value;
        }

        private void RotateIn(ReadOnlySpan<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                RotateIn(buffer[i]);
            }
        }

        public void Initialize(in ReadOnlySequence<byte> sequence)
        {
            Reset();
            if (sequence.IsSingleSegment)
            {
                RotateIn(sequence.FirstSpan());
            }
            else
            {
                foreach (var memory in sequence)
                {
                    RotateIn(memory.Span);
                }
            }
        }
    }
}
