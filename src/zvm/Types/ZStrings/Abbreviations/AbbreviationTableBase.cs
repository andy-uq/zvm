using zvm.Types.Addressing;

namespace zvm.Types.ZStrings.Abbreviations
{
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
}