using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta.Pipes
{
    internal static class StreamExtensions
    {
        public static (PipeReader, Task) ToPipeReader(
            this Stream stream,
            PipeOptions pipeOptions,
            CancellationToken ct)
        {
            var pipe = new Pipe(pipeOptions);
            var task = Task.Run(async () =>
            {
                try
                {
                    await stream.CopyToAsync(pipe.Writer, ct).ConfigureAwait(false);
                    pipe.Writer.Complete();
                }
                catch (Exception ex)
                {
                    pipe.Writer.Complete(ex);
                    throw;
                }
            });
            return (pipe.Reader, task);
        }

        public static (PipeWriter, Task) ToPipeWriter(
            this Stream stream,
            PipeOptions pipeOptions,
            CancellationToken ct)
        {
            var pipe = new Pipe(pipeOptions);
            var task = Task.Run(async () =>
            {
                try
                {
                    await pipe.Reader.CopyToAsync(stream, ct).ConfigureAwait(false);
                    pipe.Reader.Complete();
                }
                catch (Exception ex)
                {
                    pipe.Reader.Complete(ex);
                    throw;
                }
            });
            return (pipe.Writer, task);
        }

#if NETSTANDARD2_0
        public static async ValueTask<int> ReadAsync(
            this Stream stream,
            Memory<byte> memory,
            CancellationToken ct)
        {
            var array = ArrayPool<byte>.Shared.Rent(memory.Length);
            try
            {
                int read = await stream.ReadAsync(array, 0, memory.Length, ct).ConfigureAwait(false);
                array.AsSpan().Slice(0, read).CopyTo(memory.Span);
                return read;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
#endif
    }
}
