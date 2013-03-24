namespace Mega
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown when an illogical situation is detected.
	/// </summary>
	[Serializable]
	public class LogicException : Exception
	{
		public LogicException()
		{
		}

		public LogicException(string message) : base(message)
		{
		}

		public LogicException(string message, Exception inner) : base(message, inner)
		{
		}

		protected LogicException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}