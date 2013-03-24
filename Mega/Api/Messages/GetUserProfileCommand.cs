namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetUserProfileCommand
	{
		public const string CommandNameConst = "ug";

		[JsonProperty("a")]
		public readonly string CommandName = CommandNameConst;
	}
}