using System.Threading.Tasks;
using Rsync.Delta.IntegrationTests;

namespace Rsync.Delta.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tc = new TestCase(new BlockSequence._1GB(), new Mutation.IncrementAll(), i => i == 0);
            var v1 = tc.V1();
            var sig = await tc.Signature(v1);
            var v2 = tc.V2();
            var delta = await tc.Delta(sig, v2);
            var patched = await tc.Patch(delta, v1);
        }
    }
}
