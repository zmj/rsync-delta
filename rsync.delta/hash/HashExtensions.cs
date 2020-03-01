using System;
using System.Buffers;
using Rsync.Delta.Pipes;

namespace Rsync.Delta.Hash
{
    internal static class HashExtensions
    {
        public static int RotateIn<T>(
            this ref T rollingHash, 
            in ReadOnlySequence<byte> sequence)
            where T : struct, IRollingHashAlgorithm
        {
            var hashValue = rollingHash.Reset();
            if (sequence.IsSingleSegment)
            {
                rollingHash.RotateIn(sequence.FirstSpan(), ref hashValue);
            }
            else
            {
                foreach (var memory in sequence)
                {
                    rollingHash.RotateIn(memory.Span, ref hashValue);
                }
            }
            return hashValue;
        }

        private static void RotateIn<T>(
            this ref T rollingHash,
            ReadOnlySpan<byte> span,
            ref int hashValue)
            where T : struct, IRollingHashAlgorithm
        {
            for (int i = 0; i < span.Length; i++)
            {
                hashValue = rollingHash.RotateIn(span[i]);
            }
        }
    }
}
