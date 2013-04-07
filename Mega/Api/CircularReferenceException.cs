namespace Mega.Api
{
	using System;

	/// <summary>
	/// Thrown when Mega detects a circular reference in the item tree.
	/// </summary>
	public class CircularReferenceException : MegaException
	{
		public CircularReferenceException()
		{
		}

		public CircularReferenceException(string message) : base(message)
		{
		}

		public CircularReferenceException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}