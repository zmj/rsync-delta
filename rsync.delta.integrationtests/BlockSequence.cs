using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;

namespace Rsync.Delta.IntegrationTests
{
    public abstract class BlockSequence
    {
        private readonly int _blockCount;
        private readonly int _blockLength;
        private readonly int _lastBlockLength;
        private int _totalLength =>
            (_blockCount - 1) * _blockLength + _lastBlockLength;

        protected BlockSequence(
            int blockCount,
            int blockLength,
            int? lastBlockLength = null)
        {
            _blockCount = blockCount;
            _blockLength = blockLength;
            _lastBlockLength = lastBlockLength ?? blockLength;
        }

        public void WriteTo(string path)
        {
            var proc = new ProcessStartInfo("bash");
            proc.Arguments = $@"-c ""cat /dev/urandom | head -c {_totalLength} > {path}""";
            proc.Execute();
        }

        public async IAsyncEnumerable<ReadOnlySequence<byte>> Blocks(Stream file)
        {
            Debug.Assert(file.Length == _totalLength);
            var reader = PipeReader.Create(
                file,
                new StreamPipeReaderOptions(leaveOpen: true));
            for (int i = 0; i < _blockCount; i++)
            {
                int len = i == _blockCount - 1 ? _lastBlockLength : _blockLength;
                ReadOnlySequence<byte> sequence;
                do
                {
                    var readResult = await reader.ReadAsync();
                    sequence = readResult.Buffer;
                    if (sequence.Length > len)
                    {
                        sequence = sequence.Slice(0, len);
                    }
                    else if (readResult.IsCompleted && sequence.Length < len)
                    {
                        throw new Exception("unexpected EOF");
                    }
                    reader.AdvanceTo(consumed: sequence.Start, examined: sequence.End);
                } while (sequence.Length < len);
                yield return sequence;
                reader.AdvanceTo(consumed: sequence.End);
            }
            reader.Complete();
        }

        public int Count => _blockCount;

        public override string ToString() => GetType().Name.TrimStart('_');

        public static IEnumerable<BlockSequence> All()
        {
            yield return new _1KB();
            yield return new _2KB();
            yield return new _1MB();
            yield return new _1MB_Plus_1();
            yield return new _1MB_Minus_1();

            // yield return new _1GB(); // 4min
            // yield return new _10GB(); // forever
        }

        public class _1KB : BlockSequence
        {
            public _1KB() : base(
                blockCount: 1,
                blockLength: 2048,
                lastBlockLength: 1024)
            { }
        }

        public class _2KB : BlockSequence
        {
            public _2KB() : base(
                blockCount: 1,
                blockLength: 2048)
            { }
        }

        public class _1MB : BlockSequence
        {
            public _1MB() : base(
                blockCount: 512,
                blockLength: 2048)
            { }
        }

        public class _1MB_Plus_1 : BlockSequence
        {
            public _1MB_Plus_1() : base(
                blockCount: 513,
                blockLength: 2048,
                lastBlockLength: 1)
            { }
        }

        public class _1MB_Minus_1 : BlockSequence
        {
            public _1MB_Minus_1() : base(
                blockCount: 512,
                blockLength: 2048,
                lastBlockLength: 2047)
            { }
        }

        public class _1GB : BlockSequence
        {
            public _1GB() : base(
                blockCount: 524288,
                blockLength: 2048)
            { }
        }

        public class _10GB : BlockSequence
        {
            public _10GB() : base(
                blockCount: 5242880,
                blockLength: 2048)
            { }
        }
    }
}
