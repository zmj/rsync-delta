using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash.RabinKarp
{
    internal struct RabinKarp : IRollingHashAlgorithm
    {
        private const int _magic = 0x08104225;
        private const int _inverseMagic = unchecked((int)0x98F009AD);
        private const int _adjustment = 0x08104224;

        private int _value;
        private int _multiplier;

        public int Reset()
        {
            _multiplier = 1;
            return _value = 1;
        }

        public int Rotate(byte remove, byte add)
        {
            return _value = 
                _value * _magic +
                add -
                _multiplier * (remove + _adjustment);
        }

        public int RotateIn(byte add)
        {
            _multiplier *= _magic;
            return _value = _value * _magic + add;
        }

        public int RotateOut(byte remove)
        {
            _multiplier *= _inverseMagic;
            return _value -= _multiplier * (remove + _adjustment);
        }
    }
}
