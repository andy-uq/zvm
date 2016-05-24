using zvm.Types.Addressing;

namespace zvm.Types.ZStrings
{
	public struct ZStringAddress
	{
		public readonly WordAddress Address;

		public ZStringAddress(WordAddress address)
		{
			Address = address;
		}

		public ZStringAddress(ByteAddress address)
		{
			Address = new WordAddress(address);
		}

		public override string ToString() => Address.ToString();
	}
}