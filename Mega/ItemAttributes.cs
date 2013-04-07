namespace Mega
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// Represents an item attributes block, with support for encryption/decryption and serialization/deserialization.
	/// </summary>
	public sealed class ItemAttributes : Dictionary<string, string>
	{
		public ItemAttributes() : base(StringComparer.OrdinalIgnoreCase)
		{
		}

		public byte[] SerializeAndEncrypt(byte[] attributesKey)
		{
			Argument.ValidateIsNotNull(attributesKey, "attributesKey");
			Argument.ValidateLength(attributesKey, "attributesKey", 16);

			var wrapper = new JObject();

			foreach (var pair in this)
				wrapper[pair.Key] = pair.Value;

			// The Mega web frontend is unable to handle indented attribute block contents, so no formatting. Saves space, too.
			var wrapperAsString = wrapper.ToString(Formatting.None);
			var attributesString = MegaPrefix + wrapperAsString;
			var attributesBytes = Encoding.UTF8.GetBytes(attributesString);

			return Algorithms.EncryptNodeAttributes(attributesBytes, attributesKey);
		}

		public static ItemAttributes DecryptAndDeserialize(byte[] encryptedAttributesBlock, byte[] attributesKey)
		{
			Argument.ValidateIsNotNull(encryptedAttributesBlock, "encryptedAttributesBlock");
			Argument.ValidateIsNotNull(attributesKey, "attributesKey");

			Argument.ValidateLength(attributesKey, "attributesKey", 16);

			if (encryptedAttributesBlock.Length % 16 != 0)
				throw new ArgumentException("The length of the encrypted attributes block must be divisible by 16.", "encryptedAttributesBlock");

			byte[] attributesBytes = Algorithms.DecryptNodeAttributes(encryptedAttributesBlock, attributesKey);
			var attributesString = Encoding.UTF8.GetString(attributesBytes, 0, attributesBytes.Length);

			// Avoid possible data loss by refusing to work with the block if the prefix is missing.
			if (!attributesString.StartsWith(MegaPrefix))
				throw new DataIntegrityException("Attributes block does not begin with expected prefix.");

			// Remove the prefix;
			attributesString = attributesString.Substring(MegaPrefix.Length);

			var wrapper = JObject.Parse(attributesString);

			var attributes = new ItemAttributes();

			foreach (var pair in wrapper)
				attributes[pair.Key] = wrapper.Value<string>(pair.Key);

			return attributes;
		}

		/// <summary>
		/// This prefix is always stuck at the head of the attributes block string, to easily be able to verify integrity.
		/// </summary>
		private const string MegaPrefix = "MEGA";
	}
}