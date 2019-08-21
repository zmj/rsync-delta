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
}