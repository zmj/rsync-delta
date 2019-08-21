using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Rsync.Delta.Models;

namespace Rsync.Delta
{
    public interface IRsyncAlgorithm
    {
        Task GenerateSignature(
            PipeReader fileReader,
            PipeWriter signatureWriter,
            SignatureOptions? options = null,
            CancellationToken ct = default);
        
        Task GenerateSignature(
            Stream fileStream,
            PipeWriter signatureWriter,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task GenerateSignature(
            PipeReader fileReader,
            Stream signatureStream,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task GenerateSignature(
            Stream fileStream,
            Stream signatureStream,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task GenerateDelta(
            PipeReader signatureReader,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        Task GenerateDelta(
            Stream signatureStream,
            PipeReader fileReader,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        Task GenerateDelta(
            PipeReader signatureReader,
            Stream fileStream,
            PipeWriter deltaWriter,
            CancellationToken ct = default);
        
        Task GenerateDelta(
            PipeReader signatureReader,
            PipeReader fileReader,
            Stream deltaStream,
            CancellationToken ct = default);

        Task GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            PipeWriter deltaWriter,
            CancellationToken ct = default);

        Task GenerateDelta(
            Stream signatureStream,
            PipeReader fileReader,
            Stream deltaStream,
            CancellationToken ct = default);

        Task GenerateDelta(
            PipeReader signatureReader,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct = default);

        Task GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct = default);

        Task Patch(
            PipeReader deltaReader,
            Stream oldFileStream,
            PipeWriter newFileWriter,
            CancellationToken ct = default);

        Task Patch(
            Stream deltaStream,
            Stream oldFileStream,
            PipeWriter newFileWriter,
            CancellationToken ct = default);

        Task Patch(
            PipeReader deltaReader,
            Stream oldFileStream,
            Stream newFileStream,
            CancellationToken ct = default);

        Task Patch(
            Stream deltaStream,
            Stream oldFileStream,
            Stream newFileStream,
            CancellationToken ct = default);
    }

    public class RsyncAlgorithm : IRsyncAlgorithm
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly StreamPipeReaderOptions _readerOptions;
        private readonly StreamPipeWriterOptions _writerOptions;

        public RsyncAlgorithm(
            MemoryPool<byte>? memoryPool = null)
        {
            _memoryPool = memoryPool ?? MemoryPool<byte>.Shared;
            _readerOptions = new StreamPipeReaderOptions(_memoryPool);
            _writerOptions = new StreamPipeWriterOptions(_memoryPool);
        }

        public Task GenerateSignature(
            PipeReader fileReader, 
            PipeWriter signatureWriter,
            SignatureOptions? options, 
            CancellationToken ct)
        {
            if (fileReader == null)
            {
                throw new ArgumentNullException(nameof(fileReader));
            }
            if (signatureWriter == null)
            {
                throw new ArgumentNullException(nameof(signatureWriter));
            }
            return GenerateSignatureAsync();
            async Task GenerateSignatureAsync()
            {
                using var writer = new Signature.SignatureWriter(
                    fileReader, 
                    signatureWriter, 
                    options ?? SignatureOptions.Default,
                    _memoryPool);
                await writer.Write(ct);
            }
        }

        public Task GenerateSignature(
            Stream fileStream, 
            PipeWriter signatureWriter, 
            SignatureOptions? options,
            CancellationToken ct) =>
            GenerateSignature(
                PipeReader.Create(fileStream, _readerOptions),
                signatureWriter,
                options,
                ct);

        public Task GenerateSignature(
            PipeReader fileReader, 
            Stream signatureStream, 
            SignatureOptions? options, 
            CancellationToken ct) =>
            GenerateSignature(
                fileReader,
                PipeWriter.Create(signatureStream, _writerOptions),
                options,
                ct);

        public Task GenerateSignature(
            Stream fileStream, 
            Stream signatureStream, 
            SignatureOptions? options, 
            CancellationToken ct) =>
            GenerateSignature(
                PipeReader.Create(fileStream, _readerOptions),
                PipeWriter.Create(signatureStream, _writerOptions),
                options,
                ct);

        public Task GenerateDelta(
            PipeReader signatureReader, 
            PipeReader fileReader, 
            PipeWriter deltaWriter, 
            CancellationToken ct)
        {
            if (signatureReader == null) 
            {
                throw new ArgumentNullException(nameof(signatureReader)); 
            }
            if (fileReader == null)
            {
                throw new ArgumentNullException(nameof(fileReader));
            }
            if (deltaWriter == null)
            {
                throw new ArgumentNullException(nameof(deltaWriter));
            }
            return GenerateDeltaAsync();
            async Task GenerateDeltaAsync()
            {
                var reader = new Delta.SignatureReader(signatureReader, _memoryPool);
                using var matcher = await reader.Read(ct);
                var writer = new Delta.DeltaWriter(matcher, fileReader, deltaWriter);
                await writer.Write(ct);
            }
        }

        public Task GenerateDelta(
            Stream signatureStream, 
            PipeReader fileReader, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _readerOptions),
                fileReader,
                deltaWriter,
                ct);

        public Task GenerateDelta(
            PipeReader signatureReader, 
            Stream fileStream, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                PipeReader.Create(fileStream, _readerOptions),
                deltaWriter,
                ct);

        public Task GenerateDelta(
            PipeReader signatureReader, 
            PipeReader fileReader,
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                fileReader,
                PipeWriter.Create(deltaStream, _writerOptions),
                ct);

        public Task GenerateDelta(
            Stream signatureStream, 
            Stream fileStream, 
            PipeWriter deltaWriter, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _readerOptions),
                PipeReader.Create(fileStream, _readerOptions),
                deltaWriter,
                ct);

        public Task GenerateDelta(
            Stream signatureStream, 
            PipeReader fileReader, 
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _readerOptions),
                fileReader,
                PipeWriter.Create(deltaStream, _writerOptions),
                ct);

        public Task GenerateDelta(
            PipeReader signatureReader, 
            Stream fileStream, 
            Stream deltaStream, 
            CancellationToken ct) =>
            GenerateDelta(
                signatureReader,
                PipeReader.Create(fileStream, _readerOptions),
                PipeWriter.Create(deltaStream, _writerOptions),
                ct);

        public Task GenerateDelta(
            Stream signatureStream,
            Stream fileStream,
            Stream deltaStream,
            CancellationToken ct) =>
            GenerateDelta(
                PipeReader.Create(signatureStream, _readerOptions),
                PipeReader.Create(fileStream, _readerOptions),
                PipeWriter.Create(deltaStream, _writerOptions),
                ct);

        public Task Patch(
            PipeReader deltaReader, 
            Stream oldFileStream, 
            PipeWriter newFileWriter,
            CancellationToken ct = default)
        {
            if (deltaReader == null)
            {
                throw new ArgumentNullException(nameof(deltaReader));
            }
            if (oldFileStream == null)
            {
                throw new ArgumentNullException(nameof(oldFileStream));
            }
            if (newFileWriter == null)
            {
                throw new ArgumentNullException(nameof(newFileWriter));
            }
            return PatchAsync();
            async Task PatchAsync()
            {
                var copier = new Patch.Copier(oldFileStream, newFileWriter, _readerOptions);
                var patcher = new Patch.Patcher(deltaReader, newFileWriter, copier);
                await patcher.Patch(ct);
            }
        }

        public Task Patch(
            Stream deltaStream, 
            Stream oldFileStream, 
            PipeWriter newFileWriter, 
            CancellationToken ct = default) =>
            Patch(
                PipeReader.Create(deltaStream, _readerOptions),
                oldFileStream,
                newFileWriter,
                ct);

        public Task Patch(
            PipeReader deltaReader, 
            Stream oldFileStream, 
            Stream newFileStream, 
            CancellationToken ct = default) =>
            Patch(
                deltaReader,
                oldFileStream,
                PipeWriter.Create(newFileStream, _writerOptions),
                ct);

        public Task Patch(
            Stream deltaStream, 
            Stream oldFileStream, 
            Stream newFileStream, 
            CancellationToken ct = default) =>
            Patch(
                PipeReader.Create(deltaStream, _readerOptions),
                oldFileStream,
                PipeWriter.Create(newFileStream, _writerOptions),
                ct);
    }
}