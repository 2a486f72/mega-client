namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class BeginUploadCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "u";

		[JsonProperty("e")]
		public int E { get; set; }

		[JsonProperty("ms")]
		public int MS { get; set; }

		[JsonProperty("r")]
		public int R { get; set; }

		[JsonProperty("s")]
		public long Size { get; set; }

		// Also seen: ssl
	}
}