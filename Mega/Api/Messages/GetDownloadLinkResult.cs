namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetDownloadLinkResult
	{
		[JsonProperty("at")]
		public Base64Data Attributes { get; set; }

		/// <summary>
		/// You can optionally append bytes range (from and to chunk boundaries) to this URL, e.g. /0-131071
		/// </summary>
		[JsonProperty("g", Required = Required.Always)]
		public string DownloadUrl { get; set; }

		[JsonProperty("s", Required = Required.Always)]
		public long Size { get; set; }
	}
}