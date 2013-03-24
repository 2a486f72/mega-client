namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class AccountUpdatedNotification
	{
		public const string NotificationNameConst = "c";

		[JsonProperty("a")]
		public readonly string NotificationName = NotificationNameConst;

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		[JsonProperty("u")]
		public Account[] UpdatedAccounts { get; set; }
	}
}