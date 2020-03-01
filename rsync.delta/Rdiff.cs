using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Hash;
using Rsync.Delta.Hash.Adler;
using Rsync.Delta.Hash.Blake2b;
using Rsync.Delta.Hash.RabinKarp;
using Rsync.Delta.Pipes;

namespace Rsync.Delta
{
    public sealed class Rdiff : IRdiff
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly PipeOptions _pipeOptions;

        public Rdiff(MemoryPool<byte>? memoryPool = null)
        {
            _memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
            _pipeOptions = new PipeOptions(_memoryPool);
        }

        public Task SignatureAsync(
            PipeReader oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            return SignatureAsync(
                (oldFile, Task.CompletedTask),
                (signature, Task.CompletedTask),
                options ?? default,
                ct);
        }

        public Task SignatureAsync(
            Stream oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            return SignatureAsync(
                oldFile.ToPipeReader(_pipeOptions, ct),
                (signature, Task.CompletedTask),
                options ?? default,
                ct);
        }

        public Task SignatureAsync(
            PipeReader oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            return SignatureAsync(
                (oldFile, Task.CompletedTask),
                signature.ToPipeWriter(_pipeOptions, ct),
                options ?? default,
                ct);
        }

        public Task SignatureAsync(
            Stream oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            return SignatureAsync(
                oldFile.ToPipeReader(_pipeOptions, ct),
                signature.ToPipeWriter(_pipeOptions, ct),
                options ?? default,
                ct);
        }

        private async Task SignatureAsync(
            (PipeReader reader, Task task) oldFile,
            (PipeWriter writer, Task task) signature,
            SignatureOptions options,
            CancellationToken ct)
        {
            try
            {
                await ((options.RollingHashAlgorithm, options.StrongHash) switch
                {
                    (RollingHashAlgorithm.RabinKarp, StrongHashAlgorithm.Blake2b) =>
                        SignatureAsync<RabinKarp, Blake2b>(),
                    (RollingHashAlgorithm.Adler, StrongHashAlgorithm.Blake2b) =>
                        SignatureAsync<Adler32, Blake2b>(),
                    (_, StrongHashAlgorithm.Md4) => throw new NotImplementedException(),
                    _ => throw new ArgumentException($"unknown hash algorithm: {options.RollingHashAlgorithm} {options.StrongHash}")
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                oldFile.reader.Complete(ex);
                signature.writer.Complete(ex);
                throw;
            }

            async Task SignatureAsync<TRollingHashAlgorithm, TStrongHashAlgorithm>()
                where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
                where TStrongHashAlgorithm : struct, IStrongHashAlgorithm<TStrongHashAlgorithm>
            {
                using var writer = new Signature.SignatureWriter
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>(
                    oldFile.reader,
                    signature.writer,
                    options,
                    default(TRollingHashAlgorithm),
                    New<TStrongHashAlgorithm>(),
                    _memoryPool);
                await Task.WhenAll(
                    oldFile.task,
                    signature.task,
                    writer.Write(ct).AsTask())
                    .ConfigureAwait(false);
            }
        }

        public Task DeltaAsync(
            PipeReader signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                (signature, Task.CompletedTask),
                (newFile, Task.CompletedTask),
                (delta, Task.CompletedTask),
                ct);

        }

        public Task DeltaAsync(
            Stream signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct = default)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                signature.ToPipeReader(_pipeOptions, ct),
                (newFile, Task.CompletedTask),
                (delta, Task.CompletedTask),
                ct);
        }

        public Task DeltaAsync(
            PipeReader signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                (signature, Task.CompletedTask),
                newFile.ToPipeReader(_pipeOptions, ct),
                (delta, Task.CompletedTask),
                ct);
        }

        public Task DeltaAsync(
            PipeReader signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                (signature, Task.CompletedTask),
                (newFile, Task.CompletedTask),
                delta.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        public Task DeltaAsync(
            Stream signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                signature.ToPipeReader(_pipeOptions, ct),
                newFile.ToPipeReader(_pipeOptions, ct),
                (delta, Task.CompletedTask),
                ct);
        }

        public Task DeltaAsync(
            Stream signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                signature.ToPipeReader(_pipeOptions, ct),
                (newFile, Task.CompletedTask),
                delta.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        public Task DeltaAsync(
            PipeReader signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                (signature, Task.CompletedTask),
                newFile.ToPipeReader(_pipeOptions, ct),
                delta.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        public Task DeltaAsync(
            Stream signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct)
        {
            _ = signature ?? throw new ArgumentNullException(nameof(signature));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            return DeltaAsync(
                signature.ToPipeReader(_pipeOptions, ct),
                newFile.ToPipeReader(_pipeOptions, ct),
                delta.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        private async Task DeltaAsync(
            (PipeReader reader, Task task) signature,
            (PipeReader reader, Task task) newFile,
            (PipeWriter writer, Task task) delta,
            CancellationToken ct)
        {
            var reader = new Delta.SignatureReader(signature.reader, _memoryPool);
            try
            {
                var options = await reader.ReadHeader(ct).ConfigureAwait(false);
                await ((options.RollingHashAlgorithm, options.StrongHash) switch
                {
                    (RollingHashAlgorithm.RabinKarp, StrongHashAlgorithm.Blake2b) =>
                        DeltaAsync<RabinKarp, Blake2b>(options),
                    (RollingHashAlgorithm.Adler, StrongHashAlgorithm.Blake2b) =>
                        DeltaAsync<Adler32, Blake2b>(options),
                    (_, StrongHashAlgorithm.Md4) => throw new NotImplementedException(),
                    _ => throw new ArgumentException($"unknown hash algorithm: {options.RollingHashAlgorithm} {options.StrongHash}")
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                signature.reader.Complete(ex);
                newFile.reader.Complete(ex);
                delta.writer.Complete(ex);
                throw;
            }

            async Task DeltaAsync
                <TRollingHashAlgorithm, TStrongHashAlgorithm>
                (SignatureOptions options)
                where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
                where TStrongHashAlgorithm : struct, IStrongHashAlgorithm<TStrongHashAlgorithm>
            {
                var readTask = reader.ReadSignatures
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (options, ct).AsTask();
                await Task.WhenAll(readTask, signature.task).ConfigureAwait(false);
                var signatures = await readTask.ConfigureAwait(false);
                var x = New<TStrongHashAlgorithm>();
                using var matcher = new Delta.BlockMatcher
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (signatures, 
                    options,
                    default(TRollingHashAlgorithm),
                    New<TStrongHashAlgorithm>(),
                    _memoryPool);
                var writer = new Delta.DeltaWriter
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (options, matcher, newFile.reader, delta.writer);
                await Task.WhenAll(
                    newFile.task,
                    delta.task,
                    writer.Write(ct).AsTask())
                    .ConfigureAwait(false);
            }
        }

        public Task PatchAsync(
            Stream oldFile,
            PipeReader delta,
            PipeWriter newFile,
            CancellationToken ct)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            return PatchAsync(
                oldFile,
                (delta, Task.CompletedTask),
                (newFile, Task.CompletedTask),
                ct);
        }

        public Task PatchAsync(
            Stream oldFile,
            Stream delta,
            PipeWriter newFile,
            CancellationToken ct)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            return PatchAsync(
                oldFile,
                delta.ToPipeReader(_pipeOptions, ct),
                (newFile, Task.CompletedTask),
                ct);
        }

        public Task PatchAsync(
            Stream oldFile,
            PipeReader delta,
            Stream newFile,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            return PatchAsync(
                oldFile,
                (delta, Task.CompletedTask),
                newFile.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        public Task PatchAsync(
            Stream oldFile,
            Stream delta,
            Stream newFile,
            CancellationToken ct = default)
        {
            _ = oldFile ?? throw new ArgumentNullException(nameof(oldFile));
            _ = delta ?? throw new ArgumentNullException(nameof(delta));
            _ = newFile ?? throw new ArgumentNullException(nameof(newFile));
            return PatchAsync(
                oldFile,
                delta.ToPipeReader(_pipeOptions, ct),
                newFile.ToPipeWriter(_pipeOptions, ct),
                ct);
        }

        private async Task PatchAsync(
            Stream oldFile,
            (PipeReader reader, Task task) delta,
            (PipeWriter writer, Task task) newFile,
            CancellationToken ct)
        {
            try
            {
                var copier = new Patch.Copier(
                    oldFile,
                    newFile.writer);
                var patcher = new Patch.Patcher(
                    delta.reader,
                    newFile.writer,
                    copier);
                await Task.WhenAll(
                    delta.task,
                    newFile.task,
                    patcher.Patch(ct).AsTask())
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                delta.reader.Complete(ex);
                newFile.writer.Complete(ex);
                throw;
            }
        }

        private T New<T>() where T : struct, IStrongHashAlgorithm<T> =>
            default(T).New(_memoryPool);
    }
}
