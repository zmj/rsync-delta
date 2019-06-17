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
			core.HashFinal(result, isEndOfLayer: false);
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