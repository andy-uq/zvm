using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace zvm
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var storyData = new Memory(File.ReadAllBytes("minizork.z3"));

			var staticMemoryOffset = new WordAddress(14);
			var high = storyData[staticMemoryOffset.HighAddress];
			var low = storyData[staticMemoryOffset.LowAddress];

			var sizeOfDynamic = high*256 + low;
			var dynamicMemory = storyData.Slice(0, sizeOfDynamic);
			var staticMemory = storyData.Slice(sizeOfDynamic, storyData.Length - sizeOfDynamic);
			var story = new Story(dynamicMemory, staticMemory);

			Console.WriteLine($"() Loaded story data of {storyData.Length} bytes which has {dynamicMemory.Length} dynamic memory and {staticMemory.Length} of read-only.");
		}
	}

	public class Story
	{
		private readonly Memory _dynamicMemory;
		private readonly Memory _staticMemory;

		public Story(Memory dynamicMemory, Memory staticMemory)
		{
			_dynamicMemory = dynamicMemory;
			_staticMemory = staticMemory;
		}

		public int ReadByte(ByteAddress address)
		{
			if (_dynamicMemory.IsInRange(address))
			{
				return _dynamicMemory[address];
			}

			var staticAddress = address - _dynamicMemory.Length;
			return _staticMemory[staticAddress];
		}

		public int ReadWord(WordAddress wordAddress)
		{
			var high = ReadByte(wordAddress.HighAddress);
			var low = ReadByte(wordAddress.LowAddress);

			return high*256 + low;
		}

		public Story WriteByte(ByteAddress address, byte value)
		{
			var dynamic = _dynamicMemory.Write(address, value);
			return new Story(dynamic, _staticMemory);
		}

		public Story WriteWord(WordAddress address, int value)
		{
			var high = (value >> 8) & 0xff;
			var low = (value & 0xff);

			var dynamic = _dynamicMemory
				.Write(address.HighAddress, high)
				.Write(address.LowAddress, low);

			return new Story(dynamic, _staticMemory);
		}
	}

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
					throw new InvalidOperationException($"Unable to access memory above {address}");

				byte value;
				if (_edits.TryGetValue(address, out value))
					return value;

				var index = (int )address.Address;
				return _original[index];
			}
		}

		public Memory Write(ByteAddress address, int value)
		{
			if (!IsInRange(address))
				throw new InvalidOperationException($"Unable to access memory above {address}");

			var updated = _edits.SetItem(address, checked((byte )value));
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

	public class DynamicMemory { }

	public class StaticMemory { }

	public class HighMemory { }

	public struct PackedAddress
	{
		public PackedAddress(ushort address)
		{ }
	}

	public struct WordAddress
	{
		public const int WordSize = 2;

		public readonly int Address;

		public WordAddress(int address)
		{
			Address = address;
		}

		public ByteAddress HighAddress => new ByteAddress(Address);
		public ByteAddress LowAddress => new ByteAddress(Address + 1);

		public static WordAddress operator +(WordAddress address, int offset)
		{
			return new WordAddress(address.Address + (offset * WordSize));
		}

		public static WordAddress operator -(WordAddress address, int offset)
		{
			return new WordAddress(address.Address - (offset * WordSize));
		}

		public override string ToString() => $"0x{Address:x4}";
	}

	public struct ByteAddress
	{
		public readonly ushort Address;

		public ByteAddress(int address)
		{
			Address = checked ((ushort) address);
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