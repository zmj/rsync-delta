using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public interface IRdiff
    {
        Task SignatureAsync(
            PipeReader oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task SignatureAsync(
            Stream oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task SignatureAsync(
            PipeReader oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task SignatureAsync(
            Stream oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task DeltaAsync(
            PipeReader signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            Stream signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            PipeReader signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            PipeReader signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            Stream signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            Stream signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            PipeReader signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct = default);

        Task DeltaAsync(
            Stream signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct = default);

        Task PatchAsync(
            Stream oldFile,
            PipeReader delta,
            PipeWriter newFile,
            CancellationToken ct = default);

        Task PatchAsync(
            Stream oldFile,
            Stream delta,
            PipeWriter newFile,
            CancellationToken ct = default);

        Task PatchAsync(
            Stream oldFile,
            PipeReader delta,
            Stream newFile,
            CancellationToken ct = default);

        Task PatchAsync(
            Stream oldFile,
            Stream delta,
            Stream newFile,
            CancellationToken ct = default);
    }
}
