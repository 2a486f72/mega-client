namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class ItemDeletedNotification
	{
		public const string NotificationNameConst = "d";

		[JsonProperty("a")]
		public readonly string NotificationName = NotificationNameConst;

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		[JsonProperty("n")]
		public OpaqueID ItemID { get; set; }
	}
}