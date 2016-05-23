using System.Collections.Immutable;

namespace zvm.Types.ZStrings
{
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
}