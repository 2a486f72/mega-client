namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class DeleteItemCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "d";

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		[JsonProperty("n")]
		public OpaqueID ItemID { get; set; }
	}
}