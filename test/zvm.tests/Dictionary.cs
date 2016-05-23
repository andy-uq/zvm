using Xunit;
using Shouldly;
using zvm.Types.Dictionary;

namespace zvm.tests
{
	public class Dictionary
	{
		[Fact]
		public void DictionaryCount()
		{
			TestStory.Story.Dictionary.Count.ShouldBe(0x218);
		}

		[Fact]
		public void FirstEntry()
		{
			var zstring = TestStory.Story.Dictionary[new DictionaryNumber(0)];
			TestStory.Story.ReadString(zstring).ShouldBe("$ve");
		}

		[Fact]
		public void SecondEntry()
		{
			var zstring = TestStory.Story.Dictionary[new DictionaryNumber(1)];
			TestStory.Story.ReadString(zstring).ShouldBe(".");
		}

		[Fact]
		public void ThirdEntry()
		{
			var zstring = TestStory.Story.Dictionary[new DictionaryNumber(2)];
			TestStory.Story.ReadString(zstring).ShouldBe(",");
		}

		[Fact]
		public void FourthEntry()
		{
			var zstring = TestStory.Story.Dictionary[new DictionaryNumber(3)];
			TestStory.Story.ReadString(zstring).ShouldBe("#comm");
		}
	}
}