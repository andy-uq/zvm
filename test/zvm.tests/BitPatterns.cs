using System.Linq;
using Shouldly;
using Xunit;
using zvm.Types;

namespace zvm.tests
{
	public class BitPatterns
	{
		[Theory]
		[InlineData("0000 0000", 0)]
		[InlineData("1111 1111", 255)]
		[InlineData("0111 0000", 16 + 32 + 64)]
		public void CanConvertFromBitString(string bitPattern, int value)
		{
			FromBitString(bitPattern).ShouldBe(value);
		}

		[Theory]
		[InlineData("0000 0000", BitNumber.Bit0, 0)]
		[InlineData("1111 1111", BitNumber.Bit0, 1)]
		[InlineData("0111 0000", BitNumber.Bit7, 0)]
		[InlineData("0111 0000", BitNumber.Bit6, 1)]
		[InlineData("0111 0000", BitNumber.Bit5, 1)]
		public void GetBit(string source, BitNumber bitNumber, int expected)
		{
			MakeBitPattern(source).GetBit(bitNumber).ShouldBe(expected);
		}

		[Theory]
		[InlineData("0000 0000", BitNumber.Bit5, BitSize.FiveBits, "")]
		[InlineData("1111 1111", BitNumber.Bit5, BitSize.FiveBits, "1 1111")]
		[InlineData("0111 0000", BitNumber.Bit7, BitSize.ThreeBits, "011")]
		public void GetBits(string source, BitNumber bitNumber, BitSize bitSize, string expected)
		{
			MakeBitPattern(source).GetBits(bitNumber, bitSize).ShouldBe(FromBitString(expected));
		}

		private static BitPattern MakeBitPattern(string bits)
		{
			var value = FromBitString(bits);
			return new BitPattern(value);
		}

		private static int FromBitString(string bits)
		{
			int value = 0;
			int step = 1;

			foreach (var bit in bits.ToCharArray().Where(b => b == '0' || b == '1').Reverse())
			{
				if (bit == '1')
					value |= step;

				step = step * 2;
			}

			return value;
		}
	}
}
