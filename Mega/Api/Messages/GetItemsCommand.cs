namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetItemsCommand
	{
		public const string CommandNameConst = "f";

		[JsonProperty("a")]
		public readonly string CommandName = CommandNameConst;

		/// <summary>
		/// ?? 1 is used to load filesystem root
		/// </summary>
		[JsonProperty("c")]
		public int C;

		/// <summary>
		/// ?? 1 is used when right click and Reload is done on Contacts.
		/// </summary>
		[JsonProperty("r")]
		public int R;

		/// <summary>
		/// If specified, only gets the indicated item and its children.
		/// </summary>
		[JsonProperty("n")]
		public OpaqueID? ItemID { get; set; }
	}
}