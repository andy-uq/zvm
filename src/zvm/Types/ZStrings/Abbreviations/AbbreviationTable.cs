using zvm.Types.Addressing;

namespace zvm.Types.ZStrings.Abbreviations
{
	public struct AbbreviationTable
	{
		private readonly WordAddress _tableBase;

		public AbbreviationTable(int tableBase)
		{
			_tableBase = new WordAddress(tableBase);
		}

		public WordAddress this[AbbreviationNumber index] => _tableBase + index.Number;
	}
}