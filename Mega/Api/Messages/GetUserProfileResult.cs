namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class GetUserProfileResult
	{
		/// <summary>
		/// Whether the ownership of the email address has been confirmed.
		/// </summary>
		[JsonProperty("c")]
		public bool IsConfirmedAccount { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Encrypted with password key.
		/// </summary>
		[JsonProperty("k", Required = Required.Always)]
		public Base64Data MasterKey { get; set; }

		/// <summary>
		/// Array of MPIs, encrypted with master key.
		/// </summary>
		[JsonProperty("privk", Required = Required.Always)]
		public Base64Data PrivateKeyComponents { get; set; }

		/// <summary>
		/// Array of MPIs, not encrypted.
		/// </summary>
		[JsonProperty("pubk", Required = Required.Always)]
		public Base64Data PublicKeyComponents { get; set; }

		/// <summary>
		/// ??? Does not seem to be used in JavaScript.
		/// </summary>
		[JsonProperty("s")]
		public long S { get; set; }

		/// <summary>
		/// ???
		/// </summary>
		[JsonProperty("ts")]
		public Base64Data TS { get; set; }

		[JsonProperty("u", Required = Required.Always)]
		public OpaqueID UserID { get; set; }
	}
}