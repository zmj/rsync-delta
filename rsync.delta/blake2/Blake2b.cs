using System;
using System.Buffers;
using System.Diagnostics;

namespace Rsync.Delta.Blake2
{
    internal static class Blake2b
    {
        public static void Hash(ReadOnlySequence<byte> data, Span<byte> buffer)
        {
            Debug.Assert(buffer.Length <= 64);
            var core = new Core();
			core.Initialize((byte)buffer.Length);

            if (data.IsSingleSegment)
            {
                core.HashCore(data.First.Span.ToArray());
            }
            else
            {
                foreach (var memory in data)
                {
                    core.HashCore(memory.Span.ToArray());
                }
            }

			core.HashFinal(buffer, isEndOfLayer: false);
        }
    }
}