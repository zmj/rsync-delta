using System;

#nullable disable
namespace Rsync.Delta.Blake2
{
    internal class Core
    {
        private bool _isInitialized = false;
		private int _bufferFilled;
		private byte[] _buf = new byte[128];

		private ulong[] _m = new ulong[16];
		private ulong[] _h = new ulong[8];
		private ulong _counter0;
		private ulong _counter1;
		private ulong _finalizationFlag0;
		private ulong _finalizationFlag1;

		private const int NumberOfRounds = 12;
		private const int BlockSizeInBytes = 128;

		const ulong IV0 = 0x6A09E667F3BCC908UL;
		const ulong IV1 = 0xBB67AE8584CAA73BUL;
		const ulong IV2 = 0x3C6EF372FE94F82BUL;
		const ulong IV3 = 0xA54FF53A5F1D36F1UL;
		const ulong IV4 = 0x510E527FADE682D1UL;
		const ulong IV5 = 0x9B05688C2B3E6C1FUL;
		const ulong IV6 = 0x1F83D9ABFB41BD6BUL;
		const ulong IV7 = 0x5BE0CD19137E2179UL;

		private static readonly int[] Sigma = new int[NumberOfRounds * 16] {
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
			14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
			11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
			7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
			9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
			2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
			12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
			13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
			6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
			10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0,
			0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
			14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3
		};

		internal static ulong BytesToUInt64(byte[] buf, int offset)
		{
			return
				((ulong)buf[offset + 7] << 7 * 8 |
				((ulong)buf[offset + 6] << 6 * 8) |
				((ulong)buf[offset + 5] << 5 * 8) |
				((ulong)buf[offset + 4] << 4 * 8) |
				((ulong)buf[offset + 3] << 3 * 8) |
				((ulong)buf[offset + 2] << 2 * 8) |
				((ulong)buf[offset + 1] << 1 * 8) |
				((ulong)buf[offset]));
		}

		private static void UInt64ToBytes(ulong value, byte[] buf, int offset)
		{
			buf[offset + 7] = (byte)(value >> 7 * 8);
			buf[offset + 6] = (byte)(value >> 6 * 8);
			buf[offset + 5] = (byte)(value >> 5 * 8);
			buf[offset + 4] = (byte)(value >> 4 * 8);
			buf[offset + 3] = (byte)(value >> 3 * 8);
			buf[offset + 2] = (byte)(value >> 2 * 8);
			buf[offset + 1] = (byte)(value >> 1 * 8);
			buf[offset] = (byte)value;
		}

		public void Initialize(byte outputSize)
		{
			_isInitialized = true;
			
			_h[0] = IV0;
			_h[1] = IV1;
			_h[2] = IV2;
			_h[3] = IV3;
			_h[4] = IV4;
			_h[5] = IV5;
			_h[6] = IV6;
			_h[7] = IV7;

			_counter0 = 0;
			_counter1 = 0;
			_finalizationFlag0 = 0;
			_finalizationFlag1 = 0;

			_bufferFilled = 0;

			Array.Clear(_buf, 0, _buf.Length);

			Span<ulong> config = stackalloc ulong[8];
			const ulong treeIV = 0x01_01_00_00;
			config[0] = treeIV | outputSize;
			for (int i = 0; i < 8; i++)
				_h[i] ^= config[i];
		}

		public void HashCore(byte[] array, int start, int count)
		{
			if (!_isInitialized)
				throw new InvalidOperationException("Not initialized");
			if (array == null)
				throw new ArgumentNullException("array");
			if (start < 0)
				throw new ArgumentOutOfRangeException("start");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if ((long)start + (long)count > array.Length)
				throw new ArgumentOutOfRangeException("start+count");
			int offset = start;
			int bufferRemaining = BlockSizeInBytes - _bufferFilled;

			if ((_bufferFilled > 0) && (count > bufferRemaining))
			{
				Array.Copy(array, offset, _buf, _bufferFilled, bufferRemaining);
				_counter0 += BlockSizeInBytes;
				if (_counter0 == 0)
					_counter1++;
				Compress(_buf, 0);
				offset += bufferRemaining;
				count -= bufferRemaining;
				_bufferFilled = 0;
			}

			while (count > BlockSizeInBytes)
			{
				_counter0 += BlockSizeInBytes;
				if (_counter0 == 0)
					_counter1++;
				Compress(array, offset);
				offset += BlockSizeInBytes;
				count -= BlockSizeInBytes;
			}

			if (count > 0)
			{
				Array.Copy(array, offset, _buf, _bufferFilled, count);
				_bufferFilled += count;
			}
		}

		public byte[] HashFinal()
		{
			return HashFinal(false);
		}

		public byte[] HashFinal(bool isEndOfLayer)
		{
			if (!_isInitialized)
				throw new InvalidOperationException("Not initialized");
			_isInitialized = false;

			//Last compression
			_counter0 += (uint)_bufferFilled;
			_finalizationFlag0 = ulong.MaxValue;
			if (isEndOfLayer)
				_finalizationFlag1 = ulong.MaxValue;
			for (int i = _bufferFilled; i < _buf.Length; i++)
				_buf[i] = 0;
			Compress(_buf, 0);

			//Output
			byte[] hash = new byte[64];
			for (int i = 0; i < 8; ++i)
				UInt64ToBytes(_h[i], hash, i << 3);
			return hash;
		}
        private ulong[] _v = new ulong[16];

		private static ulong RotateRight(ulong value, int nBits)
		{
			return (value >> nBits) | (value << (64 - nBits));
		}

		private void G(int a, int b, int c, int d, int r, int i)
		{
			int p = (r << 4) + i;
			int p0 = Sigma[p];
			int p1 = Sigma[p + 1];
			var v = _v;
			var m = _m;

			v[a] += v[b] + m[p0];
			v[d] = RotateRight(v[d] ^ v[a], 32);
			v[c] += v[d];
			v[b] = RotateRight(v[b] ^ v[c], 24);
			v[a] += v[b] + m[p1];
			v[d] = RotateRight(v[d] ^ v[a], 16);
			v[c] += v[d];
			v[b] = RotateRight(v[b] ^ v[c], 63);
		}

		private void Compress(byte[] block, int start)
		{
			var v = _v;
			var h = _h;
			var m = _m;

			for (int i = 0; i < 16; ++i)
				m[i] = BytesToUInt64(block, start + (i << 3));

			v[0] = h[0];
			v[1] = h[1];
			v[2] = h[2];
			v[3] = h[3];
			v[4] = h[4];
			v[5] = h[5];
			v[6] = h[6];
			v[7] = h[7];

			v[8] = IV0;
			v[9] = IV1;
			v[10] = IV2;
			v[11] = IV3;
			v[12] = IV4 ^ _counter0;
			v[13] = IV5 ^ _counter1;
			v[14] = IV6 ^ _finalizationFlag0;
			v[15] = IV7 ^ _finalizationFlag1;

			for (int r = 0; r < NumberOfRounds; ++r)
			{
				G(0, 4, 8, 12, r, 0);
				G(1, 5, 9, 13, r, 2);
				G(2, 6, 10, 14, r, 4);
				G(3, 7, 11, 15, r, 6);
				G(3, 4, 9, 14, r, 14);
				G(2, 7, 8, 13, r, 12);
				G(0, 5, 10, 15, r, 8);
				G(1, 6, 11, 12, r, 10);
			}

			for (int i = 0; i < 8; ++i)
				h[i] ^= v[i] ^ v[i + 8];
		}
    }
}
#nullable restore