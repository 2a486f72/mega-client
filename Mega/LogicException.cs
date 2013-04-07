namespace Mega
{
	using System;

	/// <summary>
	/// Thrown when an illogical situation is detected.
	/// </summary>
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
	}
}