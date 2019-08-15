using System.IO.Pipelines;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta.Pipes
{
    internal static class PipeWriterExtensions
    {
        public static int Write<T>(
            this PipeWriter writer,
            T writable)
            where T : IWritable
        {
            int size = writable.Size;
            var buffer = writer.GetSpan(size);
            writable.WriteTo(buffer);
            writer.Advance(size);
            return size;
        }
    }
}