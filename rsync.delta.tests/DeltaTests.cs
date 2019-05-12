using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace Rsync.Delta.Tests
{
    public class DeltaTests
    {
        [Theory]
        [InlineData("hello_hellooo_b1")]
        public async Task Delta(string dir)
        {   
            dir = Path.GetFullPath($"../../../data/{dir}");
            byte[] expected  = await File.ReadAllBytesAsync(Path.Combine(dir, "v2.delta"));

            byte[] actual = new byte[expected.Length];
            PipeWriter writer = PipeWriter.Create(new MemoryStream(actual));
            using (var sig = File.OpenRead(Path.Combine(dir, "v1.sig")))
            using (var v2 = File.OpenRead(Path.Combine(dir, "v2.txt")))
            {
                PipeReader sigReader = PipeReader.Create(sig);
                PipeReader v2Reader = PipeReader.Create(v2);
                await new Delta2().Generate(sigReader, v2Reader, writer);
            }
            Console.WriteLine($"actual: {BitConverter.ToString(actual)}");
            Console.WriteLine($"expected: {BitConverter.ToString(expected)}");
            Assert.Equal(BitConverter.ToString(expected), BitConverter.ToString(actual));
        }
    } 
}