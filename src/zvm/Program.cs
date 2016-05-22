using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

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

			
			var zstring = new ZStringAddress(new WordAddress(0xb106));
			DumpZString(zstring, story);
		}

		private static void DumpZString(ZStringAddress zstring, Story story)
		{
			var decoder = new ZStringDecoder(story);
			Console.WriteLine(decoder.Decode(zstring));
		}
	}

	public class ZStringDecoder
	{
		private readonly Story _story;
		private readonly StringBuilder _string;

		public ZStringDecoder(Story story, StringBuilder inprogress = null)
		{
			_story = story;
			_string = inprogress ?? new StringBuilder();
		}

		public string Decode(ZStringAddress zstring)
		{
			IDecoderState state = new LowercaseCharacter(this);
			CharacterCodes(zstring)
				.Aggregate(state, (current, zchar) => current.MoveNext(zchar));

			return _string.ToString();
		}

		private IEnumerable<int> CharacterCodes(ZStringAddress zstring)
		{
			var current = zstring.Address;

			bool hasMore = true;
			while (hasMore)
			{
				var word = new BitPattern(_story.ReadWord(current));

				yield return word.Extract(BitNumber.Bit14, BitSize.FiveBits);
				yield return word.Extract(BitNumber.Bit9, BitSize.FiveBits);
				yield return word.Extract(BitNumber.Bit4, BitSize.FiveBits);

				hasMore = word.Extract(BitNumber.Bit15, BitSize.OneBit) == 0;
				current += 1;
			}
		}

		private interface IDecoderState
		{
			IDecoderState MoveNext(int zchar);
		}

		private class LowercaseCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public LowercaseCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					default:
						_parent._string.Append(Alphabet.Lowercase[zchar]);
						return this;
				}
			}
		}

		private class Abbreviation : IDecoderState
		{
			private readonly ZStringDecoder _parent;
			private readonly int _abbreviationSet;

			public Abbreviation(ZStringDecoder parent, int abbreviationSet)
			{
				_parent = parent;
				_abbreviationSet = abbreviationSet;
			}

			public IDecoderState MoveNext(int zchar)
			{
				int number = 32*(_abbreviationSet - 1) + zchar;
				var zstring = (ZStringAddress) _parent._story.Abbreviation(new AbbreviationNumber(number));

				var decoder = new ZStringDecoder(_parent._story, _parent._string);
				decoder.Decode(zstring);

				return new LowercaseCharacter(_parent);
			}
		}

		private class PunctuationCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public PunctuationCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					case 6:
						// return new AsciiCharacter(_parent);
						throw new NotImplementedException();

					default:
						_parent._string.Append(Alphabet.Punctuation[zchar]);
						return new LowercaseCharacter(_parent);
				}
			}
		}

		private class UppercaseCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public UppercaseCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					default:
						_parent._string.Append(Alphabet.Uppercase[zchar]);
						return new LowercaseCharacter(_parent);
				}
			}
		}
	}

	public struct Alphabet
	{
		public static readonly Alphabet Lowercase = new Alphabet(" ", "?", "?", "?", "?", "?", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z");
		public static readonly Alphabet Uppercase = new Alphabet(" ", "?", "?", "?", "?", "?", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z");
		public static readonly Alphabet Punctuation = new Alphabet(" ", "?", "?", "?", "?", "?", "?", "\n", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", ",", "!", "?", "_", "#", "'", "\"", "/", "\\", "-", ":", "(", ")");

		private ImmutableArray<string> _characters;
		private Alphabet(params string[] characters) : this()
		{
			_characters = characters.ToImmutableArray();
		}

		public string this[int index] => _characters[index];
	}

	public enum BitNumber
	{
		Bit0 = 0,
		Bit1,
		Bit2,
		Bit3,
		Bit4,
		Bit5,
		Bit6,
		Bit7,
		Bit8,
		Bit9,
		Bit10,
		Bit11,
		Bit12,
		Bit13,
		Bit14,
		Bit15,
	}

	public enum BitSize
	{
		OneBit = 1,
		TwoBits,
		ThreeBits,
		FourBits,
		FiveBits,
	}

	public struct BitPattern
	{
		private readonly int _word;

		public BitPattern(int value)
		{
			_word = value;
		}

		public int Extract(BitNumber high, BitSize length)
		{
			var mask = ~(-1 << (int )length);
			return (_word >> ((int) high - (int) length + 1)) & mask;
		}

		public int Extract(BitNumber bitNumber)
		{
			return Extract(bitNumber, BitSize.OneBit);
		}
	}

	public struct WordZStringAddress
	{
		private readonly int _compressedPointer;

		public WordZStringAddress(int compressedPointer)
		{
			_compressedPointer = compressedPointer;
		}

		public static explicit operator ZStringAddress(WordZStringAddress value)
		{
			var wordAddress = new WordAddress(value._compressedPointer * 2);
			return new ZStringAddress(wordAddress);
		}
	}

	public struct ZStringAddress
	{
		public readonly WordAddress Address;

		public ZStringAddress(WordAddress address)
		{
			Address = address;
		}

		public override string ToString() => Address.ToString();
	}

	public struct AbbreviationNumber
	{
		private const int MaxAbbreviations = 32 * 3;
		public readonly int Number;

		public AbbreviationNumber(int number)
		{
			if (number >= MaxAbbreviations)
				throw new ArgumentOutOfRangeException(nameof(number), $"Invalid abbreviation: {number}.  Must be less than {MaxAbbreviations}"); 

			Number = number;
		}
	}

	public struct AbbreviationTableBase
	{
		private readonly WordAddress _tableBase;

		public AbbreviationTableBase(int tableBase)
		{
			_tableBase = new WordAddress(tableBase);
		}

		public WordAddress First() => _tableBase;
		public WordAddress this[AbbreviationNumber index] => _tableBase + index.Number;
	}

	public class Story
	{
		private readonly Memory _dynamicMemory;
		private readonly Memory _staticMemory;
		private AbbreviationTableBase _abbreviationTableBase;

		public Story(Memory dynamicMemory, Memory staticMemory)
		{
			_dynamicMemory = dynamicMemory;
			_staticMemory = staticMemory;

			var tableBase = ReadWord(new WordAddress(24));
			_abbreviationTableBase = new AbbreviationTableBase(tableBase);
		}

		public WordZStringAddress Abbreviation(AbbreviationNumber number)
		{
			var address = _abbreviationTableBase[number];
			var compressedPointer = ReadWord(address);
			return new WordZStringAddress(compressedPointer);
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

		public Story Write(ByteAddress address, byte value)
		{
			var dynamic = _dynamicMemory.Write(address, value);
			return new Story(dynamic, _staticMemory);
		}

		public Story Write(WordAddress address, int value)
		{
			var dynamic = _dynamicMemory.Write(address, value);
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