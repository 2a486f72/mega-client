namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	/// <summary>
	/// Updates the attribues specified on an item.
	/// </summary>
	public sealed class SetItemAttributesCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "a";

		/// <summary>
		/// Encrypted with item attributes key, with MEGA prefix.
		/// </summary>
		[JsonProperty("attr")]
		public Base64Data Attributes { get; set; }

		[JsonProperty("n")]
		public OpaqueID ItemID { get; set; }

		/// <summary>
		/// The item key of the current user, encrypted with the user's master key.
		/// </summary>
		[JsonProperty("key")]
		public Base64Data EncryptedItemKey { get; set; }

		/// <summary>
		/// Random ID used to differentiate between connected clients, so each client can know
		/// whether some operation was performed by it or by another client application.
		/// </summary>
		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }
	}
}