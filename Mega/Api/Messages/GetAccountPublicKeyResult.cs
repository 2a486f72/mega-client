namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetAccountPublicKeyResult
	{
		[JsonProperty("pubk")]
		public Base64Data PublicKeyMpi { get; set; }
	}
}