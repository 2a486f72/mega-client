namespace Mega
{
	using System;
	using System.Linq;
	using Newtonsoft.Json;
	using Useful;

	/// <summary>
	/// Represents data stored in Mega's custom blend of base64 string encoding and facilitates easy conversion to/from.
	/// </summary>
	/// <remarks>
	/// No padding
	/// + becomes -
	/// / becomes _
	/// </remarks>
	[JsonConverter(typeof(Base64JsonConverter))]
	public struct Base64Data : IEquatable<Base64Data>
	{
		/// <summary>
		/// Gets the bytes that this object represents.
		/// </summary>
		public byte[] Bytes
		{
			get { return _bytes; }
			internal set { _bytes = value; }
		}

		public Base64Data(byte[] bytes)
		{
			Argument.ValidateIsNotNull(bytes, "bytes");

			_bytes = bytes;
		}

		/// <exception cref="FormatException">Thrown if the input data is not a valid Mega base64 string.</exception>
		public Base64Data(string encodedBytes)
		{
			Argument.ValidateIsNotNull(encodedBytes, "encodedBytes");

			_bytes = Algorithms.Base64Decode(encodedBytes);
		}

		public override string ToString()
		{
			if (Bytes == null)
				return "";

			return Algorithms.Base64Encode(Bytes);
		}

		public static implicit operator Base64Data(byte[] bytes)
		{
			return new Base64Data(bytes);
		}

		public static implicit operator Base64Data(string encodedBytes)
		{
			return new Base64Data(encodedBytes);
		}

		public static implicit operator byte[](Base64Data data)
		{
			return data.Bytes;
		}

		private byte[] _bytes;

		#region Equality
		public bool Equals(Base64Data other)
		{
			if ((_bytes == null) != (other._bytes == null))
				return false;

			if (_bytes == null)
				return true;

			return _bytes.SequenceEqual(other._bytes);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is Base64Data && Equals((Base64Data)obj);
		}

		public override int GetHashCode()
		{
			return (_bytes != null ? _bytes.Sum(b => (int)b) : 0);
		}

		public static bool operator ==(Base64Data left, Base64Data right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(Base64Data left, Base64Data right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}