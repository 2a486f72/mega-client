namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetDownloadCommand
	{
		public const string CommandNameConst = "g";

		[JsonProperty("a")]
		public readonly string CommandName = CommandNameConst;

		[JsonProperty("n")]
		public OpaqueID ItemID { get; set; }

		[JsonProperty("g")]
		public readonly int G = 1;

		// Also seen: ph
		// Also seen: ssl
	}
}