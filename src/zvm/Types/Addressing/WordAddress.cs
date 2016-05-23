using System;

namespace zvm.Types.Addressing
{
	public struct WordAddress
	{
		private const int WordSize = 2;
		private readonly int _address;

		public WordAddress(int address)
		{
			if (address < 0)
				throw new ArgumentOutOfRangeException(nameof(address), "Addresses must not be negative");

			if (address + 1 > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(address), $"Unable to create word address for 0x{address:x} because that is higher than 0x{ushort.MaxValue:x}");

			_address = address;
		}

		public WordAddress(ByteAddress byteAddress)
		{
			_address = byteAddress.Address;
		}

		public ByteAddress HighAddress => new ByteAddress(_address);
		public ByteAddress LowAddress => new ByteAddress(_address + 1);

		public static WordAddress operator +(WordAddress address, int offset)
		{
			return new WordAddress(address._address + (offset * WordSize));
		}

		public static WordAddress operator -(WordAddress address, int offset)
		{
			return new WordAddress(address._address - (offset * WordSize));
		}

		public override string ToString() => $"0x{_address:x4}";
	}
}