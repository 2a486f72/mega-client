namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class Account
	{
		[JsonProperty("m")]
		public string Email { get; set; }

		[JsonProperty("u")]
		public OpaqueID AccountID { get; set; }

		/// <summary>
		/// See KnownAccountTypes. Missing in some scenarios.
		/// </summary>
		[JsonProperty("c")]
		public int? AccountType { get; set; }

		[JsonProperty("ts")]
		public int? LastModified { get; set; }
	}
}