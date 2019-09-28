using System;
using System.IO;
using System.Threading.Tasks;

namespace Rsync.Delta.Tests
{
    internal class TempFile : IDisposable
    {
        public readonly string Name;

        public TempFile()
        {
            Directory.CreateDirectory("test");
            Name = Path.Combine("test", Guid.NewGuid().ToString());
        }

        public Stream Stream() => File.Open(
            Name,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);

        public async Task<byte[]> Bytes()
        {
            using var stream = Stream();
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }

        public void Dispose() => File.Delete(Name);
    }
}