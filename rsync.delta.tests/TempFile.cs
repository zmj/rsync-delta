using System;
using System.IO;

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

        public Stream Stream => File.Open(
            Name, 
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);

        public void Dispose() => File.Delete(Name);
    }
}