using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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