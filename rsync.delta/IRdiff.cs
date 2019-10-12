using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public interface IRdiff
    {
        Task Signature(
            PipeReader oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task Signature(
            Stream oldFile,
            PipeWriter signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task Signature(
            PipeReader oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task Signature(
            Stream oldFile,
            Stream signature,
            SignatureOptions? options = null,
            CancellationToken ct = default);

        Task Delta(
            PipeReader signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task Delta(
            Stream signature,
            PipeReader newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task Delta(
            PipeReader signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task Delta(
            PipeReader signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct = default);

        Task Delta(
            Stream signature,
            Stream newFile,
            PipeWriter delta,
            CancellationToken ct = default);

        Task Delta(
            Stream signature,
            PipeReader newFile,
            Stream delta,
            CancellationToken ct = default);

        Task Delta(
            PipeReader signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct = default);

        Task Delta(
            Stream signature,
            Stream newFile,
            Stream delta,
            CancellationToken ct = default);

        Task Patch(
            Stream oldFile,
            PipeReader delta,
            PipeWriter newFile,
            CancellationToken ct = default);

        Task Patch(
            Stream oldFile,
            Stream delta,
            PipeWriter newFile,
            CancellationToken ct = default);

        Task Patch(
            Stream oldFile,
            PipeReader delta,
            Stream newFile,
            CancellationToken ct = default);

        Task Patch(
            Stream oldFile,
            Stream delta,
            Stream newFile,
            CancellationToken ct = default);
    }
}
