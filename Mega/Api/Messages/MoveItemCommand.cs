namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class MoveItemCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "m";

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		[JsonProperty("n")]
		public OpaqueID ItemID { get; set; }

		[JsonProperty("t")]
		public OpaqueID ParentID { get; set; }
	}
}