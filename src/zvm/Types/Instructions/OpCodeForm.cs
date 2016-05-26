using System.Collections.Generic;
using System.Collections.Immutable;

namespace zvm.Types.Instructions
{
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
}