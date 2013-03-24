namespace Mega.Api
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Thrown if the item you are accessing is incomplete.
	/// </summary>
	[Serializable]
	public class IncompleteItemException : MegaException
	{
		public IncompleteItemException()
		{
		}

		public IncompleteItemException(string message) : base(message)
		{
		}

		public IncompleteItemException(string message, Exception inner) : base(message, inner)
		{
		}

		protected IncompleteItemException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}