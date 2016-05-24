using System.Linq;
using Shouldly;
using Xunit;
using zvm.Types.Objects;

namespace zvm.tests
{
	public class ObjectTables
	{
		[Fact]
		public void Count()
		{
			TestStory.Story.ObjectTable.Count.ShouldBe(179);
		}

		[Theory]
		[InlineData(1, "forest")]
		[InlineData(2, "Up a Tree")]
		[InlineData(99, "Hades")]
		[InlineData(179, "pseudo")]
		public void ObjectNames(int number, string name)
		{
			TestStory.Story.ObjectTable.Name(new ObjectNumber(number)).ShouldBe(name);
		}

		[Fact]
		public void ObjectRoots()
		{
			var roots = Enumerable.Range(1, 179)
				.Select(o => new ObjectNumber(o))
				.Where(o => TestStory.Story.ObjectTable.Parent(o) == ObjectNumber.Invalid)
				.Select(o => TestStory.Story.ObjectTable.Name(o))
				.Distinct();

			roots.ShouldBe(new[] { "<unnamed>", "you", "small piece of vitreous slag", "thing", "huge diamond", "magic boat" });
		}
	}
}