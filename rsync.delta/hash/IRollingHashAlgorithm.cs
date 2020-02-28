using System.Buffers;

namespace Rsync.Delta.Hash
{
    internal interface IRollingHashAlgorithm
    {
        int Value { get; }
        void Initialize(in ReadOnlySequence<byte> sequence);
        void Reset();
        int RotateIn(byte add);
        int Rotate(byte remove, byte add);
        int RotateOut(byte remove);
    }
}
