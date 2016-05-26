using System.Runtime.CompilerServices;

namespace zvm.Types.Instructions
{
	public struct Large
	{
		public readonly int Value;

		public Large(int value)
		{
			Value = value;
		}
	}
	public struct Small
	{
		public readonly int Value;

		public Small(int value)
		{
			Value = value;
		}
	}

	public struct LocalVariable
	{
		public readonly int Local;

		public LocalVariable(int local)
		{
			Local = local;
		}
	}

	public struct GlobalVariable
	{
		public readonly int Global;

		public GlobalVariable(int global)
		{
			Global = global;
		}
	}

	public struct VariableLocation
	{
		private readonly LocalVariable? _local;
		private readonly GlobalVariable? _global;

		public VariableLocation(LocalVariable local)
		{
			_local = local;
			_global = null;
		}

		public VariableLocation(GlobalVariable global)
		{
			_local = null;
			_global = global;
		}

		public static VariableLocation Stack() => new VariableLocation();

		public static VariableLocation Decode(int n)
		{
			if (n == 0)
				return VariableLocation.Stack();

			const int maximum_local = 15;
			return n > maximum_local 
				? new VariableLocation(new GlobalVariable(n)) 
				: new VariableLocation(new LocalVariable(n));
		}

		public int Encode()
		{
			if (_local != null)
				return _local.Value.Local;

			if (_global != null)
				return _global.Value.Global;

			return 0;
		}
	}
}