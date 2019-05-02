using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Rsync.Delta
{
    public class Signature
    {
        public Task Generate(PipeReader file, PipeWriter signature)
        {
            return Task.CompletedTask;
        }
    }
}
