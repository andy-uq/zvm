using zvm.Types.Addressing;

namespace zvm.Types.ZStrings.Abbreviations
{
	public class AbbreviationTable
	{
		private readonly AbbreviationTableOffset _tableBase;

		public AbbreviationTable(AbbreviationTableOffset tableBase)
		{
			_tableBase = tableBase;
		}

		public WordAddress this[AbbreviationNumber index] => _tableBase.Address + index.Number;
	}

	public struct AbbreviationTableOffset
	{
		public readonly WordAddress Address;

		public AbbreviationTableOffset(int tableOffset)
		{
			Address = new WordAddress(tableOffset);
		}
	}
}