using System;
using System.Buffers;

namespace Rsync.Delta.Blake2
{
    internal static class Blake2b
    {
        public static void Hash(ReadOnlySequence<byte> data, Span<byte> buffer)
        {
            var hasher = new Hasher((byte)buffer.Length);
            if (data.IsSingleSegment)
            {
                hasher.Update(data.First.Span.ToArray());
            }
            else
            {
                foreach (var memory in data)
                {
                    hasher.Update(memory.Span.ToArray());
                }
            }
            hasher.Finish(buffer);
        }
    }
}