using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rsync.Delta.IntegrationTests
{
    public abstract class Mutation
    {
        public abstract Memory<byte> Mutate(Memory<byte> block);

        public async Task WriteTo(
            IAsyncEnumerable<ReadOnlySequence<byte>> blocks,
            Func<int, bool> shouldMutate,
            Stream file)
        {
            int i = 0;
            await foreach (var block in blocks)
            {
                int length = checked((int)block.Length);
                using var lease = MemoryPool<byte>.Shared.Rent(length);
                var memory = lease.Memory[..length];
                block.CopyTo(memory.Span);
                if (shouldMutate(i))
                {
                    memory = Mutate(memory);
                }
                await file.WriteAsync(memory);
                i++;
            }
        }

        public override string ToString() => GetType().Name;

        public static IEnumerable<Mutation> All()
        {
            yield return new NoChange();
            yield return new TrimStart(1);
            yield return new TrimStart(1023);
            yield return new TrimStart(1025);
            yield return new TrimEnd(1);
            yield return new TrimEnd(1023);
            yield return new TrimEnd(1025);
            yield return new PadStart(1);
            yield return new PadStart(1023);
            yield return new PadStart(1025);
            yield return new PadEnd(1);
            yield return new PadEnd(1023);
            yield return new PadEnd(1025);
            yield return new IncrementAll();
        }

        public class NoChange : Mutation
        {
            public override Memory<byte> Mutate(Memory<byte> block) => block;
        }

        public class TrimStart : Mutation
        {
            private readonly int _n;
            public TrimStart(int n) => _n = n;
            public override Memory<byte> Mutate(Memory<byte> block)
            {
                int n = _n > block.Length ? block.Length : _n;
                return block[n..];
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        public class TrimEnd : Mutation
        {
            private readonly int _n;
            public TrimEnd(int n) => _n = n;
            public override Memory<byte> Mutate(Memory<byte> block)
            {
                int n = _n > block.Length ? block.Length : _n;
                return block[..^n];
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        public class PadStart : Mutation
        {
            private readonly int _n;
            public PadStart(int n) => _n = n;
            public override Memory<byte> Mutate(Memory<byte> block)
            {
                var mutated = new byte[_n + block.Length].AsMemory();
                int n = _n > block.Length ? block.Length : _n;
                block[^n..].CopyTo(mutated);
                block.CopyTo(mutated[_n..]);
                return mutated;
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        public class PadEnd : Mutation
        {
            private readonly int _n;
            public PadEnd(int n) => _n = n;
            public override Memory<byte> Mutate(Memory<byte> block)
            {
                var mutated = new byte[block.Length + _n].AsMemory();
                block.CopyTo(mutated);
                int n = _n > block.Length ? block.Length : _n;
                block[..n].CopyTo(mutated[block.Length..]);
                return mutated;
            }
            public override string ToString() => base.ToString() + '_' + _n;
        }

        public class IncrementAll : Mutation
        {
            public override Memory<byte> Mutate(Memory<byte> block)
            {
                var span = block.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    span[i]++;
                }
                return block;
            }
        }
    }
}
