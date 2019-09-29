using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace Rsync.Delta.IntegrationTests
{
    internal static class Extensions
    {
        public static async Task WriteTo(
            this IEnumerable<byte[]> blocks,
            Stream stream)
        {
            foreach (var block in blocks)
            {
                await stream.WriteAsync(block, offset: 0, count: block.Length);
            }
        }
    }
}