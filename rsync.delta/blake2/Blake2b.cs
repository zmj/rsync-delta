using System;

namespace Rsync.Delta.Blake2
{
    internal static class Blake2b
    {
        public static byte[] Hash(ReadOnlySpan<byte> data)
        {
            var config = new Blake2.Blake2BConfig 
            {
                OutputSizeInBytes = 32,
            };
            var hasher = new Hasher(config);
            hasher.Update(data.ToArray());
            return hasher.Finish();
        }
    }
}