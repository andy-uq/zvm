using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using zvm.Types.Addressing;

namespace zvm.Types.Instructions
{
	public static class OpCodes
	{
		public static readonly Opcode[] OneOperandBytecodes =
		{
			Opcode.OP1_128, Opcode.OP1_129, Opcode.OP1_130, Opcode.OP1_131, Opcode.OP1_132, Opcode.OP1_133,
			Opcode.OP1_134, Opcode.OP1_135, Opcode.OP1_136, Opcode.OP1_137, Opcode.OP1_138, Opcode.OP1_139,
			Opcode.OP1_140, Opcode.OP1_141, Opcode.OP1_142, Opcode.OP1_143
		};

		public static readonly Opcode[] ZeroOperandBytecodes =
		{
			Opcode.OP0_176, Opcode.OP0_177, Opcode.OP0_178, Opcode.OP0_179, Opcode.OP0_180, Opcode.OP0_181, Opcode.OP0_182, Opcode.OP0_183, Opcode.OP0_184,
			Opcode.OP0_185, Opcode.OP0_186, Opcode.OP0_187, Opcode.OP0_188, Opcode.OP0_189, Opcode.OP0_190, Opcode.OP0_191
		};

		public static readonly Opcode[] TwoOperandBytecodes =
		{
			Opcode.Illegal, Opcode.OP2_1, Opcode.OP2_2, Opcode.OP2_3, Opcode.OP2_4, Opcode.OP2_5, Opcode.OP2_6, Opcode.OP2_7,
			Opcode.OP2_8, Opcode.OP2_9, Opcode.OP2_10, Opcode.OP2_11, Opcode.OP2_12, Opcode.OP2_13, Opcode.OP2_14, Opcode.OP2_15,
			Opcode.OP2_16, Opcode.OP2_17, Opcode.OP2_18, Opcode.OP2_19, Opcode.OP2_20, Opcode.OP2_21, Opcode.OP2_22, Opcode.OP2_23,
			Opcode.OP2_24, Opcode.OP2_25, Opcode.OP2_26, Opcode.OP2_27, Opcode.OP2_28, Opcode.Illegal, Opcode.Illegal, Opcode.Illegal
		};

		public static readonly Opcode[] VarOperandBytecodes =
		{
			Opcode.VAR_224, Opcode.VAR_225, Opcode.VAR_226, Opcode.VAR_227, Opcode.VAR_228, Opcode.VAR_229, Opcode.VAR_230, Opcode.VAR_231,
			Opcode.VAR_232, Opcode.VAR_233, Opcode.VAR_234, Opcode.VAR_235, Opcode.VAR_236, Opcode.VAR_237, Opcode.VAR_238, Opcode.VAR_239,
			Opcode.VAR_240, Opcode.VAR_241, Opcode.VAR_242, Opcode.VAR_243, Opcode.VAR_244, Opcode.VAR_245, Opcode.VAR_246, Opcode.VAR_247,
			Opcode.VAR_248, Opcode.VAR_249, Opcode.VAR_250, Opcode.VAR_251, Opcode.VAR_252, Opcode.VAR_253, Opcode.VAR_254, Opcode.VAR_255
		};

		public static readonly Opcode[] ExtBytecodes =
		{
			Opcode.EXT_0, Opcode.EXT_1, Opcode.EXT_2, Opcode.EXT_3, Opcode.EXT_4, Opcode.EXT_5, Opcode.EXT_6, Opcode.EXT_7,
			Opcode.EXT_8, Opcode.EXT_9, Opcode.EXT_10, Opcode.EXT_11, Opcode.EXT_12, Opcode.EXT_13, Opcode.EXT_14, Opcode.Illegal,
			Opcode.EXT_16, Opcode.EXT_17, Opcode.EXT_18, Opcode.EXT_19, Opcode.EXT_20, Opcode.EXT_21, Opcode.EXT_22, Opcode.EXT_23,
			Opcode.EXT_24, Opcode.EXT_25, Opcode.EXT_26, Opcode.EXT_27, Opcode.EXT_28, Opcode.EXT_29, Opcode.Illegal, Opcode.Illegal
		};

		public static Operation DecodeOperation(Story story, ByteAddress address)
		{
			var firstByte = story.ReadByte(address);
			var bitPattern = new BitPattern(firstByte);

			var form = DecodeOpcodeForm(story, address);
			var operandCount = DecodeOperandCount(story, address, form);
			var opcode = DecodeOpCode(story, address, form, operandCount);

			switch (operandCount)
			{
				case OperandCount.Op0:
					return new Op0(opcode, form);

				case OperandCount.Op1:
				{
					var operandType = (OperandType) bitPattern.GetBits(BitNumber.Bit5, BitSize.TwoBits);
					return new Op1(opcode, form, operandType);
				}
			}

			if (form == OpcodeForm.Long)
			{
				switch (bitPattern.GetBits(BitNumber.Bit6, BitSize.TwoBits))
				{
					case 0:
						return new Op2(opcode, form, OperandType.Small, OperandType.Small);
					case 1:
						return new Op2(opcode, form, OperandType.Small, OperandType.Variable);
					case 2:
						return new Op2(opcode, form, OperandType.Variable, OperandType.Small);
					default:
						return new Op2(opcode, form, OperandType.Variable, OperandType.Variable);
				}
			}

			var opCodeLength = OpCodeLength(form);

			if (opcode == Opcode.VAR_236 || opcode == Opcode.VAR_250)
			{
				var typeByte0 = story.ReadByte(address + opCodeLength);
				var typeByte1 = story.ReadByte(address + (opCodeLength + 1));

				var types0 = DecodeOperandTypes(new BitPattern(typeByte0));
				var types1 = DecodeOperandTypes(new BitPattern(typeByte1));

				return new OpVar(opcode, form, types0.Concat(types1));
			}

			var typeByte = story.ReadByte(address + opCodeLength);
			return new OpVar(opcode, form, DecodeOperandTypes(new BitPattern(typeByte)));
		}

		public static IEnumerable<OperandType> DecodeOperandTypes(BitPattern typeByte)
		{
			for (var i = 3; i >= 0; i--)
			{
				var bitNumber = (BitNumber) (i*2 + 1);
				var operandType = (OperandType) typeByte.GetBits(bitNumber, BitSize.TwoBits);
				if (operandType == OperandType.Omitted)
					yield break;

				yield return operandType;
			}
		}

		public static OpcodeForm DecodeOpcodeForm(Story story, ByteAddress address)
		{
			var opcode = story.ReadByte(address);
			var bitPattern = new BitPattern(opcode);

			switch (bitPattern.GetBits(BitNumber.Bit7, BitSize.TwoBits))
			{
				case 3:
					return OpcodeForm.Variable;

				case 2:
					return opcode == 190 ? OpcodeForm.Extended : OpcodeForm.Short;

				default:
					return OpcodeForm.Long;
			}
		}

		public static OperandCount DecodeOperandCount(Story story, ByteAddress address, OpcodeForm form)
		{
			var opcode = story.ReadByte(address);
			var bitPattern = new BitPattern(opcode);

			switch (form)
			{
				case OpcodeForm.Long:
					return OperandCount.Op2;

				case OpcodeForm.Short:
					return bitPattern.GetBits(BitNumber.Bit5, BitSize.TwoBits) == 3 ? OperandCount.Op0 : OperandCount.Op1;

				case OpcodeForm.Variable:
					return bitPattern.GetBit(BitNumber.Bit5) == 1 ? OperandCount.Var : OperandCount.Op2;

				case OpcodeForm.Extended:
					return OperandCount.Var;

				default:
					throw new ArgumentOutOfRangeException(nameof(form), form, null);
			}
		}

		public static int OpCodeLength(OpcodeForm form)
		{
			return form == OpcodeForm.Extended 
				? 2 
				: 1;
		}

		public static Opcode DecodeOpCode(Story story, ByteAddress address, OpcodeForm form, OperandCount opCount)
		{
			if (form == OpcodeForm.Extended)
			{
				var ext = story.ReadByte(address + 1);
				if (ext > 29)
					return Opcode.Illegal;

				return ExtBytecodes[ext];
			}

			var opcode = story.ReadByte(address);
			var bitPattern = new BitPattern(opcode);
			switch (opCount)
			{
				case OperandCount.Op0:
					return ZeroOperandBytecodes[bitPattern.GetBits(BitNumber.Bit3, BitSize.FourBits)];

				case OperandCount.Op1:
					return OneOperandBytecodes[bitPattern.GetBits(BitNumber.Bit3, BitSize.FourBits)];

				case OperandCount.Op2:
					return TwoOperandBytecodes[bitPattern.GetBits(BitNumber.Bit4, BitSize.FiveBits)];

				case OperandCount.Var:
					return VarOperandBytecodes[bitPattern.GetBits(BitNumber.Bit4, BitSize.FiveBits)];

				default:
					throw new ArgumentOutOfRangeException(nameof(opCount), opCount, null);
			}
		}
	}

	public enum OpcodeForm
	{
		Long,
		Short,
		Variable,
		Extended
	}

	public enum OperandCount
	{
		Op0,
		Op1,
		Op2,
		Var
	}

	public abstract class Operation
	{
		protected readonly Opcode Opcode;
		protected readonly OpcodeForm Form;
		protected readonly OperandCount OperandCount;

		protected Operation(Opcode opcode, OpcodeForm form, OperandCount operandCount, ImmutableArray<OperandType> operandTypes)
		{
			OperandCount = operandCount;
			OperandTypes = operandTypes;
			Opcode = opcode;
			Form = form;
		}

		protected ImmutableArray<OperandType> OperandTypes { get; }
	}

	public class Op0 : Operation
	{
		public Op0(Opcode opcode, OpcodeForm form) : base(opcode, form, OperandCount.Op0, ImmutableArray<OperandType>.Empty)
		{
		}
	}

	public class Op1 : Operation
	{
		public OperandType OperandType { get; }

		public Op1(Opcode opcode, OpcodeForm form, OperandType operandType) : base(opcode, form, OperandCount.Op1, ImmutableArray.Create(operandType))
		{
			OperandType = operandType;
		}
	}

	public class Op2 : Operation
	{
		public OperandType Left { get; }
		public OperandType Right { get; }

		public Op2(Opcode opcode, OpcodeForm form, OperandType leftType, OperandType rightType) : base(opcode, form, OperandCount.Op2, ImmutableArray.Create(leftType, rightType))
		{
			Left = leftType;
			Right = rightType;
		}
	}

	public class OpVar : Operation
	{
		public OpVar(Opcode opcode, OpcodeForm form, IEnumerable<OperandType> operandTypes) : base(opcode, form, OperandCount.Var, operandTypes.ToImmutableArray())
		{
		}
	}

	public enum OperandType
	{
		Large,
		Small,
		Variable,
		Omitted
	}

	public enum Opcode
	{
		OP2_1,
		OP2_2,
		OP2_3,
		OP2_4,
		OP2_5,
		OP2_6,
		OP2_7,
		OP2_8,
		OP2_9,
		OP2_10,
		OP2_11,
		OP2_12,
		OP2_13,
		OP2_14,
		OP2_15,
		OP2_16,
		OP2_17,
		OP2_18,
		OP2_19,
		OP2_20,
		OP2_21,
		OP2_22,
		OP2_23,
		OP2_24,
		OP2_25,
		OP2_26,
		OP2_27,
		OP2_28,
		OP1_128,
		OP1_129,
		OP1_130,
		OP1_131,
		OP1_132,
		OP1_133,
		OP1_134,
		OP1_135,
		OP1_136,
		OP1_137,
		OP1_138,
		OP1_139,
		OP1_140,
		OP1_141,
		OP1_142,
		OP1_143,
		OP0_176,
		OP0_177,
		OP0_178,
		OP0_179,
		OP0_180,
		OP0_181,
		OP0_182,
		OP0_183,
		OP0_184,
		OP0_185,
		OP0_186,
		OP0_187,
		OP0_188,
		OP0_189,
		OP0_190,
		OP0_191,
		VAR_224,
		VAR_225,
		VAR_226,
		VAR_227,
		VAR_228,
		VAR_229,
		VAR_230,
		VAR_231,
		VAR_232,
		VAR_233,
		VAR_234,
		VAR_235,
		VAR_236,
		VAR_237,
		VAR_238,
		VAR_239,
		VAR_240,
		VAR_241,
		VAR_242,
		VAR_243,
		VAR_244,
		VAR_245,
		VAR_246,
		VAR_247,
		VAR_248,
		VAR_249,
		VAR_250,
		VAR_251,
		VAR_252,
		VAR_253,
		VAR_254,
		VAR_255,
		EXT_0,
		EXT_1,
		EXT_2,
		EXT_3,
		EXT_4,
		EXT_5,
		EXT_6,
		EXT_7,
		EXT_8,
		EXT_9,
		EXT_10,
		EXT_11,
		EXT_12,
		EXT_13,
		EXT_14,
		EXT_16,
		EXT_17,
		EXT_18,
		EXT_19,
		EXT_20,
		EXT_21,
		EXT_22,
		EXT_23,
		EXT_24,
		EXT_25,
		EXT_26,
		EXT_27,
		EXT_28,
		EXT_29,
		Illegal
	}
}