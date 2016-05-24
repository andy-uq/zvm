using zvm.Types.Addressing;
using zvm.Types.ZStrings;

namespace zvm.Types.Dictionary
{
	public struct DictionaryBase
	{
		public readonly ByteAddress Address;
		public readonly int EntrySize;
		public readonly WordAddress WordAddress;

		public DictionaryBase(ByteAddress address, int entrySize)
		{
			Address = address;
			EntrySize = entrySize;
			WordAddress = new WordAddress(Address);
		}

		public DictionaryAddress this[DictionaryNumber index] => new DictionaryAddress(Address, EntrySize, index);
	}

	public struct DictionaryTableOffset
	{
		public readonly ByteAddress Address;

		public DictionaryTableOffset(int tableOffset)
		{
			Address = new ByteAddress(tableOffset);
		}
	}

	public class DictionaryTable
	{
		private readonly ByteAddress _wordSeparators;
		private readonly int _wordSeparatorsCount;

		private readonly DictionaryBase _entryBase;
		private readonly int _entryCount;

		public DictionaryTable(Story story, DictionaryTableOffset tableOffset)
		{
			_wordSeparators = tableOffset.Address;
			_wordSeparatorsCount = story.ReadByte(_wordSeparators);

			ByteAddress dictionaryBase = _wordSeparators + _wordSeparatorsCount + 1;
			var entrySize = story.ReadByte(dictionaryBase);

			_entryBase = new DictionaryBase(dictionaryBase + 3, entrySize);
			_entryCount = story.ReadWord(new WordAddress(dictionaryBase + 1));
		}

		public int Count => _entryCount;

		public ZStringAddress this[DictionaryNumber index] => this[_entryBase[index]];
		public ZStringAddress this[DictionaryAddress address] => new ZStringAddress(address.Address);
	}

	public struct DictionaryAddress
	{
		public readonly WordAddress Address;

		public DictionaryAddress(ByteAddress dictionaryBase, int entryLength, DictionaryNumber dictionaryNumber)
		{
			var byteAddress = dictionaryBase + dictionaryNumber.Number * entryLength;
			Address = new WordAddress(byteAddress);
		}
	}

	public struct DictionaryNumber
	{
		public readonly int Number;

		public DictionaryNumber(int number)
		{
			Number = number;
		}
	}
}