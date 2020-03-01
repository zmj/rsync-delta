﻿using System;
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

        public Task Signature(
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
                options ?? SignatureOptions.Default,
                ct);
        }

        public Task Signature(
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
                options ?? SignatureOptions.Default,
                ct);
        }

        public Task Signature(
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
                options ?? SignatureOptions.Default,
                ct);
        }

        public Task Signature(
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
                options ?? SignatureOptions.Default,
                ct);
        }

        private Task SignatureAsync(
            (PipeReader reader, Task task) oldFile,
            (PipeWriter writer, Task task) signature,
            SignatureOptions options,
            CancellationToken ct)
        {
            return (options.RollingHash, options.StrongHash) switch
            {
                (RollingHashAlgorithm.RabinKarp, StrongHashAlgorithm.Blake2b) =>
                    SignatureAsync(new RabinKarp(), new Blake2b(_memoryPool)),
                (RollingHashAlgorithm.Adler, StrongHashAlgorithm.Blake2b) =>
                    SignatureAsync(new Adler32(), new Blake2b(_memoryPool)),
                (_, StrongHashAlgorithm.Md4) => throw new NotImplementedException(),
                _ => throw new ArgumentException($"unknown hash algorithm: {options.RollingHash} {options.StrongHash}")
            };

            async Task SignatureAsync
                <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (TRollingHashAlgorithm rollingHashAlgorithm,
                    TStrongHashAlgorithm strongHashAlgorithm)
                where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
                where TStrongHashAlgorithm : IStrongHashAlgorithm
            {
                using var writer = new Signature.SignatureWriter
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>(
                    oldFile.reader,
                    signature.writer,
                    options,
                    rollingHashAlgorithm,
                    strongHashAlgorithm,
                    _memoryPool);
                await Task.WhenAll(
                    oldFile.task,
                    signature.task,
                    writer.Write(ct).AsTask())
                    .ConfigureAwait(false);
            }
        }

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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

        public Task Delta(
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
            var options = await reader.ReadHeader(ct).ConfigureAwait(false);
            await ((options.RollingHash, options.StrongHash) switch
            {
                (RollingHashAlgorithm.RabinKarp, StrongHashAlgorithm.Blake2b) =>
                    DeltaAsync(new RabinKarp(), new Blake2b(_memoryPool)),
                (RollingHashAlgorithm.Adler, StrongHashAlgorithm.Blake2b) =>
                    DeltaAsync(new Adler32(), new Blake2b(_memoryPool)),
                (_, StrongHashAlgorithm.Md4) => throw new NotImplementedException(),
                _ => throw new ArgumentException($"unknown hash algorithm: {options.RollingHash} {options.StrongHash}")
            }).ConfigureAwait(false);

            async Task DeltaAsync
                <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (TRollingHashAlgorithm rollingHashAlgorithm,
                    TStrongHashAlgorithm strongHashAlgorithm)
                where TRollingHashAlgorithm : struct, IRollingHashAlgorithm
                where TStrongHashAlgorithm : IStrongHashAlgorithm
            {
                var readTask = reader.ReadSignatures
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (options, ct).AsTask();
                await Task.WhenAll(readTask, signature.task).ConfigureAwait(false);
                var signatures = await readTask.ConfigureAwait(false);
                using var matcher = new Delta.BlockMatcher
                    <TRollingHashAlgorithm, TStrongHashAlgorithm>
                    (signatures, options, rollingHashAlgorithm, strongHashAlgorithm, _memoryPool);
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

        public Task Patch(
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

        public Task Patch(
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

        public Task Patch(
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

        public Task Patch(
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
    }
}
