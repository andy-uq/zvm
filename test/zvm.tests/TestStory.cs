using System;
using System.IO;
using zvm.Types.Addressing;

namespace zvm.tests
{
	public static class TestStory
	{
		private static readonly Lazy<Story> _story;

		static TestStory()
		{
			_story = new Lazy<Story>(() => LoadStory());
		}

		public static Story Story => _story.Value;

		private static Story LoadStory()
		{
			var storyData = new Memory(File.ReadAllBytes("../../minizork.z3"));

			var staticMemoryOffset = new WordAddress(14);
			var high = storyData[staticMemoryOffset.HighAddress];
			var low = storyData[staticMemoryOffset.LowAddress];

			var sizeOfDynamic = high*256 + low;
			var dynamicMemory = storyData.Slice(0, sizeOfDynamic);
			var staticMemory = storyData.Slice(sizeOfDynamic, storyData.Length - sizeOfDynamic);
			return new Story(dynamicMemory, staticMemory);
		}
	}
}