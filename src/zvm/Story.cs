using zvm.Types.Addressing;
using zvm.Types.Dictionary;
using zvm.Types.ZStrings;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm
{
	public class Story
	{
		private readonly Memory _dynamicMemory;
		private readonly Memory _staticMemory;
		private AbbreviationTable _abbreviationTable;

		public Story(Memory dynamicMemory, Memory staticMemory)
		{
			_dynamicMemory = dynamicMemory;
			_staticMemory = staticMemory;

			var abbreviationTableOffset = ReadWord(new WordAddress(24));
			_abbreviationTable = new AbbreviationTable(abbreviationTableOffset);

			var dictionaryTableOffset = ReadWord(new WordAddress(8));
			Dictionary = new DictionaryTable(this, dictionaryTableOffset);
		}

		public DictionaryTable Dictionary { get; }

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
}