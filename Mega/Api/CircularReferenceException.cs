namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown when Mega detects a circular reference in the item tree.
	/// </summary>
	[Serializable]
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

		protected CircularReferenceException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}