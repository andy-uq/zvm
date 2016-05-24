using System;
using zvm.Types.Addressing;
using zvm.Types.ZStrings;

namespace zvm.Types.Objects
{
	public class ObjectTable
	{
		private ObjectTree _objectTree;

		public ObjectTable(Story story, ObjectTableOffset tableOffset)
		{
			var defaultPropertyTableSize = story.IsV3 ? 31 : 63;
			var defaultPropertyEntrySize = 2;

			var treeBase = tableOffset.Address + defaultPropertyEntrySize * defaultPropertyTableSize;
			var sizeOfEntry = story.IsV3 ? 9 : 14;
			_objectTree = new ObjectTree(story, treeBase, sizeOfEntry);
		}

		public int Count => _objectTree.Count();
		public string Name(ObjectNumber number) => _objectTree.Name(number);
		public ObjectNumber Parent(ObjectNumber number) => _objectTree.Parent(number);
	}

	public class ObjectTree
	{
		private readonly Story _story;
		private readonly ByteAddress _treeBase;
		private readonly int _sizeOfEntry;

		public ObjectTree(Story story, ByteAddress treeBase, int sizeOfEntry)
		{
			_story = story;
			_treeBase = treeBase;
			_sizeOfEntry = sizeOfEntry;
		}

		public ObjectAddress this[ObjectNumber number] => new ObjectAddress(_treeBase + (number.Number - 1)*_sizeOfEntry);

		public ObjectNumber Parent(ObjectNumber number)
		{
			if (_story.IsV3)
			{
				var address = this[number];
				var parent = _story.ReadByte(address.Address + 4);
				return new ObjectNumber(parent);
			}

			return new ObjectNumber(0);
		}

		public ObjectNumber Sibling(ObjectNumber number)
		{
			if (_story.IsV3)
			{
				var address = this[number];
				var parent = _story.ReadByte(address.Address + 5);
				return new ObjectNumber(parent);
			}

			return new ObjectNumber(0);
		}

		public ObjectNumber Child(ObjectNumber number)
		{
			if (_story.IsV3)
			{
				var address = this[number];
				var parent = _story.ReadByte(address.Address + 6);
				return new ObjectNumber(parent);
			}

			return new ObjectNumber(0);
		}

		public PropertyDataOffset PropertyData(ObjectNumber number)
		{
			if (_story.IsV3)
			{
				var address = this[number];
				var propertyDataOffset = _story.ReadWord(new WordAddress(address.Address + 7));
				return new PropertyDataOffset(propertyDataOffset);
			}

			return new PropertyDataOffset(0);
		}

		public string Name(ObjectNumber number)
		{
			var propertyDataOffset = PropertyData(number);
			int length = _story.ReadByte(propertyDataOffset.Address);
			return length == 0
				? "<unnamed>"
				: _story.ReadString(new ZStringAddress(propertyDataOffset.Address + 1));
		}

		public int Count()
		{
			var propertyDataOffset = PropertyData(new ObjectNumber(1));
			var length = (int )propertyDataOffset.Address - _treeBase.Address;
			return length/_sizeOfEntry;
		}
	}

	public struct PropertyDataOffset
	{
		public readonly ByteAddress Address;

		public PropertyDataOffset(int address)
		{
			Address = new ByteAddress(address);
		}
	}

	public struct ObjectTableOffset
	{
		public readonly ByteAddress Address;

		public ObjectTableOffset(int tableOffset)
		{
			Address = new ByteAddress(tableOffset);
		}
	}

	public struct ObjectAddress
	{
		public readonly ByteAddress Address;

		public ObjectAddress(ByteAddress address)
		{
			Address = address;
		}
	}

	public struct ObjectNumber : IEquatable<ObjectNumber>
	{
		public readonly int Number;

		public ObjectNumber(int number)
		{
			Number = number;
		}

		public static readonly ObjectNumber Invalid = new ObjectNumber(0);

		public bool Equals(ObjectNumber other)
		{
			return Number == other.Number;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ObjectNumber && Equals((ObjectNumber) obj);
		}

		public override int GetHashCode()
		{
			return Number;
		}

		public static bool operator ==(ObjectNumber left, ObjectNumber right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ObjectNumber left, ObjectNumber right)
		{
			return !left.Equals(right);
		}
	}
}