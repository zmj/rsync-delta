using System;

#nullable disable
namespace Rsync.Delta.Blake2
{
    public sealed class Blake2BConfig : ICloneable
	{
		public byte[] Personalization { get; set; }
		public byte[] Salt { get; set; }
		public byte[] Key { get; set; }
		public int OutputSizeInBytes { get; set; }
		public int OutputSizeInBits
		{
			get { return OutputSizeInBytes * 8; }
			set
			{
				if (value % 8 != 0)
					throw new ArgumentException("Output size must be a multiple of 8 bits");
				OutputSizeInBytes = value / 8;
			}
		}

		public Blake2BConfig()
		{
			OutputSizeInBytes = 64;
		}

		public Blake2BConfig Clone()
		{
			var result = new Blake2BConfig();
			result.OutputSizeInBytes = OutputSizeInBytes;
			if (Key != null)
				result.Key = (byte[])Key.Clone();
			if (Personalization != null)
				result.Personalization = (byte[])Personalization.Clone();
			if (Salt != null)
				result.Salt = (byte[])Salt.Clone();
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}

    	public sealed class Blake2BTreeConfig : ICloneable
	{
		public int IntermediateHashSize { get; set; }
		public int MaxHeight { get; set; }
		public long LeafSize { get; set; }
		public int FanOut { get; set; }

		public Blake2BTreeConfig()
		{
			IntermediateHashSize = 64;
		}

		public Blake2BTreeConfig Clone()
		{
			var result = new Blake2BTreeConfig();
			result.IntermediateHashSize = IntermediateHashSize;
			result.MaxHeight = MaxHeight;
			result.LeafSize = LeafSize;
			result.FanOut = FanOut;
			return result;
		}

		public static Blake2BTreeConfig CreateInterleaved(int parallelism)
		{
			var result = new Blake2BTreeConfig();
			result.FanOut = parallelism;
			result.MaxHeight = 2;
			result.IntermediateHashSize = 64;
			return result;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
#nullable restore