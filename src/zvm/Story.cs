using System;
using System.Linq;
using zvm.Types.Addressing;
using zvm.Types.Dictionary;
using zvm.Types.ZStrings;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm
{
	public enum StoryVersion : byte
	{
		V3 = 3
	}

	public class Story
	{
		private readonly Memory _dynamicMemory;
		private readonly Memory _staticMemory;
		private AbbreviationTable _abbreviationTable;

		public Story(Memory dynamicMemory, Memory staticMemory)
		{
			_dynamicMemory = dynamicMemory;
			_staticMemory = staticMemory;

			StoryVersion[] validVersions = (StoryVersion[]) Enum.GetValues(typeof(StoryVersion));
			Version = (StoryVersion) ReadByte(new ByteAddress(0));
			if (validVersions.All(v => v != Version))
			{
				throw new InvalidStoryVersionException($"Invalid story version {Version}");
			}

			var abbreviationTableOffset = ReadWord(new WordAddress(24));
			_abbreviationTable = new AbbreviationTable(abbreviationTableOffset);

			var dictionaryTableOffset = ReadWord(new WordAddress(8));
			Dictionary = new DictionaryTable(this, dictionaryTableOffset);
		}

		public DictionaryTable Dictionary { get; }
		public StoryVersion Version { get; }

		public WordZStringAddress Abbreviation(AbbreviationNumber number)
		{
			var address = _abbreviationTable[number];
			var compressedPointer = ReadWord(address);
			return new WordZStringAddress(compressedPointer);
		}

		public string ReadString(ZStringAddress zstring)
		{
			var decoder = new ZStringDecoder(this);
			return decoder.Decode(zstring);
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

	public class InvalidStoryVersionException : InvalidOperationException
	{
		public InvalidStoryVersionException(string message) : base(message)
		{
		}
	}
}