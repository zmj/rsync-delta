using System;
using System.Buffers;

namespace Rsync.Delta.Hash
{
    internal static class HashExtensions
    {
        public static int RotateIn<T>(
            this ref T rollingHash, 
            in ReadOnlySequence<byte> sequence)
            where T : IRollingHashAlgorithm
        {
            throw new NotImplementedException();
        }
    }
}
