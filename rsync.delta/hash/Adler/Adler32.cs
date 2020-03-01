using System;
using System.Buffers;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash.Adler
{
    internal struct Adler32 : IRollingHashAlgorithm
    {
        private const byte _magic = 31;

        private int Value => (_b << 16) | _a;

        private ushort _a;
        private ushort _b;
        private uint _count;

        public int Reset()
        {
            _a = 0;
            _b = 0;
            _count = 0;
            return Value;
        }

        public int Rotate(byte remove, byte add)
        {
            _a += (ushort)(add - remove);
            _b += (ushort)(_a - _count * (remove + _magic));
            return Value;
        }

        public int RotateIn(byte add)
        {
            _a += (ushort)(add + _magic);
            _b += _a;
            _count++;
            return Value;
        }

        public int RotateOut(byte remove)
        {
            _a -= (ushort)(remove + _magic);
            _b -= (ushort)(_count * (remove + _magic));
            _count--;
            return Value;
        }
    }
}
