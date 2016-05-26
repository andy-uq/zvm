using System;
using System.Collections.Generic;
using System.Linq;
using zvm.Types.Addressing;

namespace zvm.Types.Instructions
{
	public struct Operand
	{
		private readonly Large? _large;
		private readonly Small? _small;
		private VariableLocation? _variable;

		public Operand(Large large)
		{
			_large = large;
			_small = null;
			_variable = null;
		}

		public Operand(Small small)
		{
			_large = null;
			_small = small;
			_variable = null;
		}

		public Operand(VariableLocation variable)
		{
			_large = null;
			_small = null;
			_variable = variable;
		}
	}

	public class OperandDecoder
	{
		private readonly Story _story;
		private ByteAddress _operandAddress;

		private OperandDecoder(Story story, ByteAddress operandAddress)
		{
			_story = story;
			_operandAddress = operandAddress;
		}

		public static IEnumerable<Operand> DecodeOperands(Story story, ByteAddress operandAddress, IEnumerable<OperandType> operandTypes)
		{
			var decoder = new OperandDecoder(story, operandAddress);
			return operandTypes.Select(decoder.Decode);
		}

		public static int OperandLength(IEnumerable<OperandType> operandTypes)
		{
			return operandTypes.Sum(OperandLength);
		}

		private static int OperandLength(OperandType operandType)
		{
			return operandType == OperandType.Large ? 2 : 1;
		}

		private Operand Decode(OperandType operandType)
		{
			switch (operandType)
			{
				case OperandType.Large:
				{
					var word = _story.ReadWord(new WordAddress(_operandAddress));
					_operandAddress += 2;
					return new Operand(new Large(word));
				}

				case OperandType.Small:
				{
					var b = _story.ReadByte(_operandAddress);
					_operandAddress += 1;

					return new Operand(new Small(b));
				}

				case OperandType.Variable:
				{
					var b = _story.ReadByte(_operandAddress);
					_operandAddress += 1;

					var v = VariableLocation.Decode(b);
					return new Operand(v);
				}

				case OperandType.Omitted:
				default:
					throw new ArgumentOutOfRangeException(nameof(operandType));
			}
		}
	}
}