using System;

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
		private readonly byte[] key;
		private readonly int outputSizeInBytes;
		private static readonly Blake2BConfig DefaultConfig = new Blake2BConfig();

		public void Init()
		{
			core.Initialize(rawConfig);
			if (key != null)
			{
				core.HashCore(key, 0, key.Length);
			}
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

		public Hasher(Blake2BConfig config)
		{
			if (config == null)
				config = DefaultConfig;
			rawConfig = IvBuilder.ConfigB(config, null);
			if (config.Key != null && config.Key.Length != 0)
			{
				key = new byte[128];
				Array.Copy(config.Key, key, config.Key.Length);
			}
			outputSizeInBytes = config.OutputSizeInBytes;
			Init();
		}

		public void Update(byte[] data, int start, int count)
		{
			core.HashCore(data, start, count);
        }
    }
}
#nullable restore