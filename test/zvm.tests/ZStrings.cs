using System;
using System.Linq;
using Shouldly;
using Xunit;
using zvm.Types.ZStrings;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm.tests
{
	public class ZStrings
	{
		private readonly Story _story = TestStory.Story;

		[Fact]
		public void CanMakeZString()
		{
			ZStringDecoder decoder = new ZStringDecoder(_story);
			var abbreviation = (ZStringAddress) _story.Abbreviation(new AbbreviationNumber(0));
			decoder.Decode(abbreviation).ShouldBe("the ");
		}

		[Fact]
		public void DecodeAbbreviationTable()
		{
			var decoder = new ZStringDecoder(_story);
			var abbreviations =
				from n in Enumerable.Range(0, AbbreviationNumber.MaxAbbreviations)
				let abbreviation = new AbbreviationNumber(n)
				let address = (ZStringAddress) _story.Abbreviation(abbreviation)
				select new { number = abbreviation, value = decoder.Decode(address)}
				;

			foreach (var abbreviation in abbreviations)
			{
				Console.WriteLine($"Abbreviation {abbreviation.number}");
				Console.WriteLine(abbreviation.value);
			}
		}
	}
}