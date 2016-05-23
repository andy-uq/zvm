namespace zvm.Types
{
	public struct BitPattern
	{
		private readonly int _word;

		public BitPattern(int value)
		{
			_word = value;
		}

		public int GetBits(BitNumber high, BitSize length)
		{
			var mask = ~(-1 << (int )length);
			return (_word >> ((int) high - (int) length + 1)) & mask;
		}

		public int GetBit(BitNumber bitNumber)
		{
			return GetBits(bitNumber, BitSize.OneBit);
		}
	}
}