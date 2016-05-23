using System;
using Shouldly;
using Xunit;
using zvm.Types.Addressing;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm.tests
{
	public class Abbreviations
	{
		private readonly Story _story = TestStory.Story;

		[Fact]
		public void ReadFirstAbbreviation()
		{
			var abbreviation = (WordAddress)_story.Abbreviation(new AbbreviationNumber(0));
			abbreviation.HighAddress.Address.ShouldBe(0x40);
		}

		[Fact]
		public void ReadSecondAbbreviation()
		{
			var abbreviation = (WordAddress)_story.Abbreviation(new AbbreviationNumber(1));
			abbreviation.HighAddress.Address.ShouldBe(0x44);
		}

		[Fact]
		public void ReadLastAbbreviation()
		{
			var abbreviation = (WordAddress)_story.Abbreviation(new AbbreviationNumber(95));
			abbreviation.HighAddress.Address.ShouldBe(0x1f0);
		}

		[Fact]
		public void GoOverMaximumAbbreviations()
		{
			Should.Throw<ArgumentOutOfRangeException>(() => new AbbreviationNumber(AbbreviationNumber.MaxAbbreviations));
		}
	}
}