using System;

namespace zvm.Types.Addressing
{
	public struct ByteAddress
	{
		public readonly int Address;

		public ByteAddress(int address)
		{
			if (address < 0)
				throw new ArgumentOutOfRangeException(nameof(address), "Addresses must not be negative");

			if (address > ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(address), $"Unable to create address for 0x{address:x} because that is higher than 0x{ushort.MaxValue:x}");

			Address = checked ((ushort) address);
		}

		public static explicit operator int(ByteAddress address)
		{
			return address.Address;
		}

		public static ByteAddress operator +(ByteAddress address, int offset)
		{
			return new ByteAddress(address.Address + offset);
		}

		public static ByteAddress operator -(ByteAddress address, int offset)
		{
			return new ByteAddress(address.Address - offset);
		}

		public override string ToString() => $"0x{Address:x4}";
	}
}