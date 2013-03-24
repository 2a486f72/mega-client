namespace Mega.Api.Messages
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	/// <summary>
	/// Wraps a response from the incoming notification service. This may either contain a set of notifications or a URL to wait on.
	/// </summary>
	public sealed class IncomingNotificationWrapper
	{
		[JsonProperty("a")]
		public JObject[] Notifications { get; set; }

		/// <summary>
		/// Present if we should perform another request for incoming commands right away.
		/// </summary>
		[JsonProperty("sn")]
		public Base64Data? NextSequenceReference { get; set; }

		/// <summary>
		/// Present if we should wait on this URL before performing another request with the current sequence reference.
		/// </summary>
		[JsonProperty("w")]
		public string WaitUrl { get; set; }
	}
}