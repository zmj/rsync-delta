using System;
using System.Buffers;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash.Adler
{
    internal struct RollingHash
    {
        private const byte _magic = 31;

        public readonly int Value => (_b << 16) | _a;

        private ushort _a;
        private ushort _b;
        private uint _count;

        public void Rotate(byte remove, byte add)
        {
            _a += (ushort)(add - remove);
            _b += (ushort)(_a - _count * (remove + _magic));
        }

        private void RotateIn(byte add)
        {
            _a += (ushort)(add + _magic);
            _b += _a;
            _count++;
        }

        public void RotateOut(byte remove)
        {
            _a -= (ushort)(remove + _magic);
            _b -= (ushort)(_count * (remove + _magic));
            _count--;
        }

        private void RotateIn(in ReadOnlySpan<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                RotateIn(buffer[i]);
            }
        }

        public void RotateIn(in ReadOnlySequence<byte> sequence)
        {
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
