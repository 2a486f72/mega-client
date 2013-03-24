namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class BeginUploadResult
	{
		[JsonProperty("p")]
		public string UploadUrl { get; set; }
	}
}