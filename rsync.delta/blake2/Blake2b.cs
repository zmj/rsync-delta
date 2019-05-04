using System;

namespace Rsync.Delta.Blake2
{
    internal static class Blake2b
    {
        public static void Hash(ReadOnlySpan<byte> data, Span<byte> hash)
        {
            var hasher = new Hasher(null);
            hasher.Update(data.ToArray());
            byte[] h = hasher.Finish();
            h.CopyTo(hash);
        }
    }
}