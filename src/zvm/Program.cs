using System;
using System.IO;
using zvm.Types.Addressing;
using zvm.Types.ZStrings;

namespace zvm
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var storyData = new Memory(File.ReadAllBytes("minizork.z3"));

			var staticMemoryOffset = new WordAddress(14);
			var high = storyData[staticMemoryOffset.HighAddress];
			var low = storyData[staticMemoryOffset.LowAddress];

			var sizeOfDynamic = high*256 + low;
			var dynamicMemory = storyData.Slice(0, sizeOfDynamic);
			var staticMemory = storyData.Slice(sizeOfDynamic, storyData.Length - sizeOfDynamic);
			var story = new Story(dynamicMemory, staticMemory);
			Console.WriteLine($"() Loaded story data of {storyData.Length} bytes which has {dynamicMemory.Length} dynamic memory and {staticMemory.Length} of read-only.");

			
			var zstring = new ZStringAddress(new WordAddress(0xb106));
			DumpZString(zstring, story);
		}

		private static void DumpZString(ZStringAddress zstring, Story story)
		{
			var decoder = new ZStringDecoder(story);
			Console.WriteLine(decoder.Decode(zstring));
		}
	}
}