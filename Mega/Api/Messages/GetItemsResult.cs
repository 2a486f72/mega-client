namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetItemsResult
	{
		[JsonProperty("f", Required = Required.Always)]
		public Item[] Items { get; set; }

		[JsonProperty("u")]
		public Account[] KnownAccounts { get; set; }

		/// <summary>
		/// The sequence reference is needed to connect to the S2C pipeline. It is delivered together with the filesystem
		/// so that the server can know from which point on to send notifications on filesystem changes. Pass this value to
		/// Channel.IncomingSequenceReference to enable it to process S2C notifications.
		/// </summary>
		[JsonProperty("sn")]
		public Base64Data IncomingSequenceReference { get; set; }
	}
}