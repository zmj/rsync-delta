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

        public Stream Read(TestFile filename) =>
            File.Open(Path(filename), FileMode.Open, FileAccess.Read);

        public Stream Write(TestFile filename) =>
            File.Open(Path(filename), FileMode.Create, FileAccess.Write);

        public string Path(TestFile filename) => 
            System.IO.Path.Combine(_path, filename.ToString());

        public void Dispose()
        {
            Directory.Delete(_path, recursive: true);
        }
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