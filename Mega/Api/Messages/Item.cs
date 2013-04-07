namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class Item
	{
		/// <summary>
		/// Encrypted JSON object, prefixed with MEGA and zero-padded.
		/// In this object "n" stands for name of the node.
		/// 
		/// AES-CBC encrypted with KeyPart1 XOR KeyPart2.
		/// </summary>
		[JsonProperty("a", Required = Required.Always)]
		public Base64Data Attributes { get; set; }

		/// <summary>
		/// Unique ID of the node.
		/// </summary>
		[JsonProperty("h", Required = Required.Always)]
		public OpaqueID ID { get; set; }

		/// <summary>
		/// One or more keys that can be used to decrypt the item. The keys are encrypted(!).
		/// Each key carries a SourceID identifying where it comes from (user ID or share ID) - this info
		/// is meant to help you figure out what key to use to decrypt the node key.
		/// 
		/// File key is 32 bytes, folder key is 16 bytes (KeyPart2 is missing, since folders do not have a content stream).
		/// KeyPart1 is 16 bytes of AES key.
		/// KeyPart2 (files only) is 8 bytes nonce and 8 bytes Meta-MAC.
		/// 
		/// To decrypt file contents, use KeyPart1 XOR KeyPart2 as the key.
		/// 
		/// File attributes use AES-CBC, data uses AES-CTR, where the CTR (see algorithm for details) is:
		/// nonce(8) + file-position-derived-value-1(4) + file-position-derived-value-2(4)
		/// </summary>
		[JsonProperty("k", Required = Required.Always)]
		[JsonConverter(typeof(EncryptedItemKeySetConverter))]
		public EncryptedItemKey[] EncryptedKeys { get; set; }

		[JsonProperty("p", Required = Required.AllowNull)]
		public OpaqueID? ParentID { get; set; }

		/// <summary>
		/// If this is the root of a share, this is the amount of access we have to the share.
		/// See KnownShareAccessLevels.
		/// </summary>
		[JsonProperty("r")]
		public int? ShareAccessLevel { get; set; }

		/// <summary>
		/// If this is the root of a share, this is the master key of the share, RSA-encrypted with your public key(?).
		/// </summary>
		[JsonProperty("sk")]
		public Base64Data? ShareKey { get; set; }

		/// <summary>
		/// If this is the root of a share, this is the ID of the share's owner.
		/// </summary>
		[JsonProperty("su")]
		public OpaqueID? ShareOwner { get; set; }

		/// <summary>
		/// Size.
		/// </summary>
		[JsonProperty("s")]
		public long? Size { get; set; }

		/// <summary>
		/// See KnownNodeTypes class.
		/// </summary>
		[JsonProperty("t", Required = Required.Always)]
		public int Type { get; set; }

		[JsonProperty("ts", Required = Required.Always)]
		public int Timestamp { get; set; }

		[JsonProperty("u", Required = Required.Always)]
		public OpaqueID OwnerID { get; set; }
	}
}