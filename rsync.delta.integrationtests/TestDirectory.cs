using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Rsync.Delta.IntegrationTests
{
    internal class TestDirectory : IDisposable
    {
        private readonly string _path;

        public TestDirectory(
            string testName,
            BlockSequence blocks,
            Mutation mutation)
        {
            _path = $"testdata/{testName}_{blocks}_{mutation}";
            Directory.CreateDirectory(_path);
        }

        public async Task Write(TestFile filename, IEnumerable<byte[]> blocks)
        {
            using var file = Write(filename);
            foreach (var block in blocks)
            {
                await file.WriteAsync(block.AsMemory());
            }
        }

        public Stream Read(TestFile filename)
        {
            var path = Path.Combine(_path, filename.ToString());
            return File.Open(path, FileMode.Open, FileAccess.Read);
        }

        public Stream Write(TestFile filename)
        {
            var path = Path.Combine(_path, filename.ToString());
            return File.Open(path, FileMode.Create, FileAccess.Write);
        }

        public void Dispose() => Directory.Delete(_path, recursive: true);
    }

    public enum TestFile
    {
        v1,
        v2,
        sig,
        delta,
        patched,
        rs_sig,
        rs_delta,
    }
}