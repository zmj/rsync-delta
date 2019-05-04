using System;

namespace Rsync.Delta.Blake2
{
    internal static class Blake2b
    {
        public static void Hash(ReadOnlySpan<byte> data, Span<byte> hash)
        {
            var config = new Blake2.Blake2BConfig 
            {
                OutputSizeInBytes = 32,
            };
            var hasher = new Hasher(config);
            hasher.Update(data.ToArray());
            byte[] h = hasher.Finish();
            h.CopyTo(hash);
        }
    }
}