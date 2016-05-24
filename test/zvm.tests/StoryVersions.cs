using System;
using Shouldly;
using Xunit;

namespace zvm.tests
{
	public class StoryVersions
	{
		[Fact]
		public void InvalidVersion()
		{
			Should.Throw<InvalidStoryVersionException>(() => new Story(new Memory(new byte[] { 0 }), new Memory(new byte[0])));
		}

		[Fact]
		public void ValidVersion()
		{
			TestStory.Story.Version.ShouldBe(StoryVersion.V3);
		}
	}
}