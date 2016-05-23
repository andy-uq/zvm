using System;

namespace zvm.Types.ZStrings.Abbreviations
{
	public struct AbbreviationNumber
	{
		public const int MaxAbbreviations = 32 * 3;
		public readonly int Number;

		public AbbreviationNumber(int number)
		{
			if (number < 0)
				throw new ArgumentOutOfRangeException(nameof(number), $"Invalid abbreviation: {number}.  Must be greater than or equal to zero");

			if (number >= MaxAbbreviations)
				throw new ArgumentOutOfRangeException(nameof(number), $"Invalid abbreviation: {number}.  Must be less than {MaxAbbreviations}"); 

			Number = number;
		}

		public override string ToString() => $"Abbrev:{Number}";
	}
}