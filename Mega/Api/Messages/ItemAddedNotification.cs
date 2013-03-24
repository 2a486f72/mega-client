namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class ItemAddedNotification
	{
		public const string NotificationNameConst = "t";

		[JsonProperty("a")]
		public readonly string NotificationName = NotificationNameConst;

		[JsonProperty("i")]
		public OpaqueID ClientInstanceID { get; set; }

		[JsonProperty("t")]
		public ItemTree AddedItemTree { get; set; }

		public sealed class ItemTree
		{
			[JsonProperty("f")]
			public Item[] Items { get; set; }

			[JsonProperty("u")]
			public Account[] RelatedAccounts { get; set; }
		}
	}
}