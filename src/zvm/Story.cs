using System;
using System.Linq;
using zvm.Types.Addressing;
using zvm.Types.Dictionary;
using zvm.Types.Objects;
using zvm.Types.ZStrings;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm
{
	public enum StoryVersion : byte
	{
		V1 = 1,
		V2,
		V3,
		V4,
		V5,
		V6,
		V7
	}

	public static class StoryVersionMethods
	{
		public static bool IsV3OrLower(this StoryVersion version) => version <= StoryVersion.V3;
		public static bool IsV4OrLower(this StoryVersion version) => version <= StoryVersion.V4;
		public static bool IsV4OrHigher(this StoryVersion version) => version >= StoryVersion.V4;
		public static bool IsV5OrHigher(this StoryVersion version) => version >= StoryVersion.V5;
	}

	public class Story
	{
		private readonly Memory _dynamicMemory;
		private readonly Memory _staticMemory;

		private readonly AbbreviationTable _abbreviationTable;

		public static class StoryHeaderOffsets
		{
			public static readonly ByteAddress StoryVersion = new ByteAddress(0);
			public static readonly WordAddress AbbreviationTableBase = new WordAddress(24);
			public static readonly WordAddress DictionaryTableBase = new WordAddress(8);
			public static readonly WordAddress ObjectTableOffset = new WordAddress(10);
		}

		public Story(Memory dynamicMemory, Memory staticMemory)
		{
			_dynamicMemory = dynamicMemory;
			_staticMemory = staticMemory;

			var header = new StoryHeader(this);
			Version = header.Version;
			_abbreviationTable = new AbbreviationTable(header.AbbreviationTableOffset);
			Dictionary = new DictionaryTable(this, header.DictionaryTableOffset);
			ObjectTable = new ObjectTable(this, header.ObjectTableOffset);
		}

		public DictionaryTable Dictionary { get; }
		public StoryVersion Version { get; }
		public ObjectTable ObjectTable { get; }

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

		public class StoryHeader
		{
			public StoryHeader(Story story)
			{
				Version = (StoryVersion)story.ReadByte(StoryHeaderOffsets.StoryVersion);

				var validVersions = (StoryVersion[])Enum.GetValues(typeof(StoryVersion));
				if (validVersions.All(v => v != Version))
				{
					throw new InvalidStoryVersionException($"Invalid story version {Version}");
				}

				AbbreviationTableOffset = new AbbreviationTableOffset(story.ReadWord(StoryHeaderOffsets.AbbreviationTableBase));
				DictionaryTableOffset = new DictionaryTableOffset(story.ReadWord(StoryHeaderOffsets.DictionaryTableBase));
				ObjectTableOffset = new ObjectTableOffset(story.ReadWord(StoryHeaderOffsets.ObjectTableOffset));
			}

			public StoryVersion Version { get; }
			public DictionaryTableOffset DictionaryTableOffset { get; }
			public AbbreviationTableOffset AbbreviationTableOffset { get; }
			public ObjectTableOffset ObjectTableOffset { get; }
		}
	}

	public class InvalidStoryVersionException : InvalidOperationException
	{
		public InvalidStoryVersionException(string message) : base(message)
		{
		}
	}
}