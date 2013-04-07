namespace Mega.Api.Messages
{
	using System.Collections.Generic;
	using Newtonsoft.Json;

	/// <summary>
	/// Updates the keys for a bunch of items. This is a good idea when you receive items that have public-key-encrypted keys.
	/// For speed of access, it makes sense to replace them with master-key-encrypted keys, which can be decrypted much faster.
	/// </summary>
	public sealed class SetItemKeysCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "k";

		/// <summary>
		/// Item N is item ID. Item N+1 is item key. Repeat for more items. Seriously.
		/// </summary>
		[JsonProperty("nk")]
		public List<Base64Data> ItemsAndNewKeys { get; set; }

		public SetItemKeysCommand()
		{
			ItemsAndNewKeys = new List<Base64Data>();
		}
	}
}