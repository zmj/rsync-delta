using System;
using System.Buffers;

namespace Rsync.Delta.Hash
{
    internal static class HashAlgorithmFactory
    {
        public static IRollingHashAlgorithm Create(
            RollingHashAlgorithm alg) =>
            alg switch
            {
                RollingHashAlgorithm.RabinKarp => throw new NotImplementedException(),
                RollingHashAlgorithm.Adler => new Adler.Adler32(),
                _ => throw new ArgumentException($"unknown {nameof(RollingHashAlgorithm)}.{alg}")
            };

        public static IStrongHashAlgorithm Create(
            StrongHashAlgorithm alg,
            MemoryPool<byte> memoryPool) =>
            alg switch
            {
                StrongHashAlgorithm.Blake2b => new Blake2b.Blake2b(memoryPool),
                StrongHashAlgorithm.Md4 => throw new NotImplementedException(),
                _ => throw new ArgumentException($"unknown {nameof(StrongHashAlgorithm)}.{alg}")
            };
    }
}
