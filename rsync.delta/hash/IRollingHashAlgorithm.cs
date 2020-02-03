using System.Buffers;

namespace Rsync.Delta.Hash
{
    internal interface IRollingHashAlgorithm
    {
        int Value { get; }
        void Initialize(in ReadOnlySequence<byte> sequence);
        void Rotate(byte remove, byte add);
        void RotateOut(byte remove);
    }
}
