using System;
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
    }
}
