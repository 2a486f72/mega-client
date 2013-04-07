namespace Mega
{
	using System;
	using Newtonsoft.Json;
	using Useful;

	/// <summary>
	/// Lets you easily treat a byte array as an opaque ID, with comparison operators and all that.
	/// </summary>
	[JsonConverter(typeof(Base64JsonConverter))]
	public struct OpaqueID : IEquatable<OpaqueID>
	{
		public static readonly OpaqueID None = new OpaqueID(new byte[0]);

		/// <summary>
		/// Gets the raw binary data comprising this ID.
		/// </summary>
		public Base64Data BinaryData { get; internal set; }

		public OpaqueID(Base64Data binaryData) : this()
		{
			BinaryData = binaryData;
		}

		public OpaqueID(byte[] bytes) : this()
		{
			BinaryData = bytes;
		}

		public OpaqueID(string encodedBytes) : this()
		{
			BinaryData = encodedBytes;
		}

		public static implicit operator byte[](OpaqueID instance)
		{
			Argument.ValidateIsNotNull(instance, "instance");

			return instance.BinaryData;
		}

		public static implicit operator OpaqueID(byte[] bytes)
		{
			Argument.ValidateIsNotNull(bytes, "bytes");

			return new OpaqueID(bytes);
		}

		public static implicit operator OpaqueID(string encodedBytes)
		{
			Argument.ValidateIsNotNull(encodedBytes, "encodedBytes");

			return new OpaqueID(encodedBytes);
		}

		public override string ToString()
		{
			return BinaryData.ToString();
		}

		private static readonly Random _random = new Random();

		public static OpaqueID Random(int length)
		{
			Argument.ValidateRange(length, "length", 0, int.MaxValue);

			byte[] bytes = new byte[length];
			_random.NextBytes(bytes);

			return bytes;
		}

		#region Equality members
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is OpaqueID && Equals((OpaqueID)obj);
		}

		public override int GetHashCode()
		{
			return BinaryData.GetHashCode();
		}

		public bool Equals(OpaqueID other)
		{
			return BinaryData.Equals(other.BinaryData);
		}

		public static bool operator ==(OpaqueID left, OpaqueID right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(OpaqueID left, OpaqueID right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}