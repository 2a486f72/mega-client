namespace Mega
{
	using System;

	/// <summary>
	/// A key used to protect the data of a node, together with the ID of whatever provided the key.
	/// Keys can be user keys or share keys - the SourceID will point to the user or share the key is from.
	/// </summary>
	public struct EncryptedItemKey : IEquatable<EncryptedItemKey>
	{
		/// <summary>
		/// Could be a user ID or share ID.
		/// </summary>
		public OpaqueID SourceID { get; private set; }

		/// <summary>
		/// Encrypted with user or share master key, depending on what the source is.
		/// </summary>
		public Base64Data EncryptedKey { get; private set; }

		public EncryptedItemKey(OpaqueID sourceID, Base64Data encryptedKey) : this()
		{
			SourceID = sourceID;
			EncryptedKey = encryptedKey;
		}

		public override string ToString()
		{
			if (SourceID == default(OpaqueID) && EncryptedKey == default(Base64Data))
				return "";

			return SourceID.ToString() + ":" + EncryptedKey.ToString();
		}

		#region Equality
		public bool Equals(EncryptedItemKey other)
		{
			return SourceID.Equals(other.SourceID) && EncryptedKey.Equals(other.EncryptedKey);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is EncryptedItemKey && Equals((EncryptedItemKey)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (SourceID.GetHashCode() * 397) ^ EncryptedKey.GetHashCode();
			}
		}

		public static bool operator ==(EncryptedItemKey left, EncryptedItemKey right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(EncryptedItemKey left, EncryptedItemKey right)
		{
			return !left.Equals(right);
		}
		#endregion
	}
}