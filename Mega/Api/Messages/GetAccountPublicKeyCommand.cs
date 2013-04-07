namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetAccountPublicKeyCommand
	{
		public const string CommandNameConst = "uk";

		[JsonProperty("a")]
		public readonly string CommandName = CommandNameConst;

		[JsonProperty("u")]
		public OpaqueID AccountID { get; set; }
	}
}