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
            StreamPipeReaderOptions streamOptions,
            CancellationToken ct)
        {
#if !NETSTANDARD2_0
            var pipe = new Pipe(pipeOptions);
            var task = Task.Run(async () => 
            {
                try
                {                    
                    int read;
                    FlushResult flushResult;
                    do
                    {
                        var memory = pipe.Writer.GetMemory(streamOptions.BufferSize);
                        read = await stream.ReadAsync(memory, ct).ConfigureAwait(false);
                        pipe.Writer.Advance(read);
                        flushResult = await pipe.Writer.FlushAsync(ct).ConfigureAwait(false);
                    } while (
                        read > 0 && 
                        !flushResult.IsCompleted &&
                        !ct.IsCancellationRequested);
                    pipe.Writer.Complete();
                }
                catch (Exception ex)
                {
                    pipe.Writer.Complete(ex);
                    throw;
                }
            }); // no ct to ensure the pipe is completed
            return (pipe.Reader, task);
#else
            return (PipeReader.Create(stream, streamOptions), Task.CompletedTask);
#endif
        }

        public static (PipeWriter, Task) ToPipeWriter(
            this Stream stream,
            PipeOptions pipeOptions,
            StreamPipeWriterOptions streamOptions,
            CancellationToken ct)
        {
#if !NETSTANDARD2_0
            var pipe = new Pipe(pipeOptions);
            var task = Task.Run(async () =>
            {
                try
                {
                    ReadResult readResult;
                    do
                    {
                        readResult = await pipe.Reader.ReadAsync(ct).ConfigureAwait(false);
                        foreach (var memory in readResult.Buffer)
                        {
                            await stream.WriteAsync(memory, ct).ConfigureAwait(false);
                        }
                        pipe.Reader.AdvanceTo(readResult.Buffer.End);
                    } while (
                        !readResult.IsCompleted &&
                        !ct.IsCancellationRequested);
                    pipe.Reader.Complete();
                }
                catch (Exception ex)
                {
                    pipe.Reader.Complete(ex);
                    throw;
                }
            }); // no ct to ensure the pipe is completed
            return (pipe.Writer, task);
#else
            return (PipeWriter.Create(stream, streamOptions), Task.CompletedTask);
#endif
        }
    }
}
