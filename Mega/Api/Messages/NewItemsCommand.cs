namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class NewItemsCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "p";

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		/// <summary>
		/// Either a local container item or an account ID (if sending file to account).
		/// </summary>
		[JsonProperty("t")]
		public OpaqueID ParentID { get; set; }

		[JsonProperty("n")]
		public NewItem[] Items { get; set; }

		public sealed class NewItem
		{
			/// <summary>
			/// Dummy upload completion token that must be used when creating folders.
			/// </summary>
			public static readonly Base64Data FolderUploadCompletionToken = "xxxxxxxx";

			/// <summary>
			/// Encrypted JSON object, prefixed with MEGA and zero-padded.
			/// In this object "n" stands for name of the node.
			/// 
			/// AES-CBC encrypted with KeyPart1 XOR KeyPart2.
			/// </summary>
			[JsonProperty("a")]
			public Base64Data Attributes { get; set; }

			/// <summary>
			/// The item key of the account that the file is located under, encrypted with the account's master key.
			/// If this is a file in your own cloud filesystem, this is encrypted with your own master key.
			/// If this is a file in some other account's cloud filesystem, this is encrypted with the account public key.
			/// </summary>
			[JsonProperty("k")]
			public Base64Data EncryptedItemKey { get; set; }

			/// <summary>
			/// See KnownNodeTypes class.
			/// </summary>
			[JsonProperty("t", Required = Required.Always)]
			public int Type { get; set; }

			/// <summary>
			/// Completion token from upload process or dummy constant for folders (FolderUploadCompletionToken)
			/// or source node ID for local content being shared to other accounts.
			/// </summary>
			[JsonProperty("h")]
			public Base64Data ItemContentsReference { get; set; }

			public NewItem()
			{
				ItemContentsReference = FolderUploadCompletionToken;
			}
		}
	}
}