namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class NewItemsCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "p";

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

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
			/// The item key of the current user, encrypted with the user's master key.
			/// </summary>
			[JsonProperty("k")]
			public Base64Data EncryptedItemKey { get; set; }

			/// <summary>
			/// See KnownNodeTypes class.
			/// </summary>
			[JsonProperty("t", Required = Required.Always)]
			public int Type { get; set; }

			/// <summary>
			/// Completion token from upload process or dummy constant for folders (FolderUploadCompletionToken).
			/// </summary>
			[JsonProperty("h")]
			public Base64Data UploadCompletionToken { get; set; }

			public NewItem()
			{
				UploadCompletionToken = FolderUploadCompletionToken;
			}
		}
	}
}