using System;
using System.Diagnostics;

#nullable disable
namespace Rsync.Delta.Blake2
{
    internal class Hasher
    {
		public void Update(byte[] data)
		{
			core.HashCore(data, 0, data.Length);
		}

        private readonly Core core = new Core();
		private readonly int outputSizeInBytes;
		
		public void Finish(Span<byte> result)
		{
			Debug.Assert(result.Length == outputSizeInBytes);
			var fullResult = core.HashFinal().AsSpan();
            if (outputSizeInBytes < fullResult.Length)
			{
				fullResult = fullResult.Slice(0, result.Length);
			}
			fullResult.CopyTo(result);
		}

		public Hasher(byte outputSize)
		{
			Debug.Assert(outputSize <= 64);
			core.Initialize(outputSize);
			outputSizeInBytes = outputSize;
		}
    }
}
#nullable restore