using zvm.Types.Addressing;

namespace zvm.Types.ZStrings
{
	public struct WordZStringAddress
	{
		private readonly int _compressedPointer;

		public WordZStringAddress(int compressedPointer)
		{
			_compressedPointer = compressedPointer;
		}

		public static explicit operator ZStringAddress(WordZStringAddress value)
		{
			var wordAddress = (WordAddress )value;
			return new ZStringAddress(wordAddress);
		}

		public static explicit operator WordAddress(WordZStringAddress value)
		{
			var wordAddress = new WordAddress(value._compressedPointer*2);
			return wordAddress;
		}
	}
}