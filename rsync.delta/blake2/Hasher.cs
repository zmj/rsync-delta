using System;
using System.Diagnostics;

#nullable disable
namespace Rsync.Delta.Blake2
{
    internal class Hasher
    {
		public void Update(byte[] data)
		{
			Update(data, 0, data.Length);
		}

        private readonly Core core = new Core();
		private readonly ulong[] rawConfig;
		private readonly int outputSizeInBytes;
		
		public void Init()
		{
			core.Initialize(rawConfig);
		}

		public byte[] Finish()
		{
			var fullResult = core.HashFinal();
			if (outputSizeInBytes != fullResult.Length)
			{
				var result = new byte[outputSizeInBytes];
				Array.Copy(fullResult, result, result.Length);
				return result;
			}
			else return fullResult;
		}

		public Hasher(int outputSize)
		{
			Debug.Assert(outputSize <= 64);
			rawConfig = new ulong[8];
			IvBuilder.ConfigB(outputSize, rawConfig);
			outputSizeInBytes = outputSize;
			Init();
		}

		public void Update(byte[] data, int start, int count)
		{
			core.HashCore(data, start, count);
        }
    }
}
#nullable restore