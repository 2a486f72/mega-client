namespace Mega.Client
{
	using System;

	/// <summary>
	/// Thrown if an attempt is made to load a cloud filesystem item that is not usable.
	/// For example, if you do not have a key for it or it contains conflicting data.
	/// </summary>
	[Serializable]
	public class UnusableItemException : Exception
	{
		public UnusableItemException()
		{
		}

		public UnusableItemException(string message) : base(message)
		{
		}

		public UnusableItemException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}