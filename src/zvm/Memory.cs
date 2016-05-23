using System;
using System.Collections.Immutable;
using zvm.Types.Addressing;

namespace zvm
{
	public class Memory
	{
		private readonly ImmutableList<byte> _original;
		private readonly ImmutableDictionary<ByteAddress, byte> _edits;

		public Memory(byte[] initial)
		{
			_original = initial.ToImmutableList();
			_edits = ImmutableDictionary<ByteAddress, byte>.Empty;
		}

		private Memory(ImmutableList<byte> initial, ImmutableDictionary<ByteAddress, byte> edits)
		{
			_original = initial;
			_edits = edits;
		}

		public int Length => _original.Count;

		public int this[ByteAddress address]
		{
			get
			{
				if (!IsInRange(address))
					throw new InvalidOperationException($"Unable to access memory above 0x{Length:x4} (Requested {address})");

				byte value;
				if (_edits.TryGetValue(address, out value))
					return value;

				var index = (int )address.Address;
				return _original[index];
			}
		}

		public int this[WordAddress wordAddress]
		{
			get
			{
				var high = this[wordAddress.HighAddress];
				var low = this[wordAddress.LowAddress];

				return high*256 + low;
			}
		}

		public Memory Write(ByteAddress address, int value)
		{
			if (!IsInRange(address))
				throw new InvalidOperationException($"Unable to access memory above {address}");

			var updated = _edits.SetItem(address, checked((byte )value));
			return new Memory(_original, updated);
		}

		public Memory Write(WordAddress address, int value)
		{
			if (!IsInRange(address.HighAddress))
				throw new InvalidOperationException($"Unable to access memory above {address}");

			var high = (value >> 8) & 0xff;
			var low = (value & 0xff);

			var updated = _edits
				.SetItem(address.HighAddress, checked((byte) high))
				.SetItem(address.LowAddress, checked((byte) low));

			return new Memory(_original, updated);
		}

		public bool IsInRange(ByteAddress address)
		{
			return address.Address < Length;
		}

		public Memory Slice(int start, int length)
		{
			var slice = _original.GetRange(start, length);
			return new Memory(slice, ImmutableDictionary<ByteAddress, byte>.Empty);
		}
	}
}