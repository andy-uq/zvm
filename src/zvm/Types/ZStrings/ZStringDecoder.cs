using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using zvm.Types.ZStrings.Abbreviations;

namespace zvm.Types.ZStrings
{
	public class ZStringDecoder
	{
		private readonly Story _story;
		private readonly StringBuilder _string;

		public ZStringDecoder(Story story, StringBuilder inprogress = null)
		{
			_story = story;
			_string = inprogress ?? new StringBuilder();
		}

		public string Decode(ZStringAddress zstring)
		{
			IDecoderState state = new LowercaseCharacter(this);
			CharacterCodes(zstring)
				.Aggregate(state, (current, zchar) => current.MoveNext(zchar));

			return _string.ToString();
		}

		private IEnumerable<int> CharacterCodes(ZStringAddress zstring)
		{
			var current = zstring.Address;

			bool hasMore = true;
			while (hasMore)
			{
				var word = new BitPattern(_story.ReadWord(current));

				yield return word.GetBits(BitNumber.Bit14, BitSize.FiveBits);
				yield return word.GetBits(BitNumber.Bit9, BitSize.FiveBits);
				yield return word.GetBits(BitNumber.Bit4, BitSize.FiveBits);

				hasMore = word.GetBits(BitNumber.Bit15, BitSize.OneBit) == 0;
				current += 1;
			}
		}

		private interface IDecoderState
		{
			IDecoderState MoveNext(int zchar);
		}

		private class LowercaseCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public LowercaseCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					default:
						_parent._string.Append(Alphabet.Lowercase[zchar]);
						return this;
				}
			}
		}

		private class Abbreviation : IDecoderState
		{
			private readonly ZStringDecoder _parent;
			private readonly int _abbreviationSet;

			public Abbreviation(ZStringDecoder parent, int abbreviationSet)
			{
				_parent = parent;
				_abbreviationSet = abbreviationSet;
			}

			public IDecoderState MoveNext(int zchar)
			{
				int number = 32*(_abbreviationSet - 1) + zchar;
				var zstring = (ZStringAddress) _parent._story.Abbreviation(new AbbreviationNumber(number));

				var decoder = new ZStringDecoder(_parent._story, _parent._string);
				decoder.Decode(zstring);

				return new LowercaseCharacter(_parent);
			}
		}

		private class PunctuationCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public PunctuationCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					case 6:
						// return new AsciiCharacter(_parent);
						throw new NotImplementedException();

					default:
						_parent._string.Append(Alphabet.Punctuation[zchar]);
						return new LowercaseCharacter(_parent);
				}
			}
		}

		private class UppercaseCharacter : IDecoderState
		{
			private readonly ZStringDecoder _parent;

			public UppercaseCharacter(ZStringDecoder parent)
			{
				_parent = parent;
			}

			public IDecoderState MoveNext(int zchar)
			{
				switch (zchar)
				{
					case 1:
					case 2:
					case 3:
						return new Abbreviation(_parent, zchar);

					case 4:
						return new UppercaseCharacter(_parent);

					case 5:
						return new PunctuationCharacter(_parent);

					default:
						_parent._string.Append(Alphabet.Uppercase[zchar]);
						return new LowercaseCharacter(_parent);
				}
			}
		}
	}
}