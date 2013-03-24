namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	/// <summary>
	/// Provides us encrypted data for the user session. We prove our ownership of the account by decrypting this date
	/// and extracting from it the session ID that we need in order to make further API calls to Mega.
	/// </summary>
	public sealed class OpenUserSessionResult
	{
		/// <summary>
		/// RSA-encrypted bytes stuck in an MPI. The first 43 bytes are the session ID.
		/// </summary>
		[JsonProperty("csid", Required = Required.Always)]
		public Base64Data SessionIDData { get; set; }

		/// <summary>
		/// Encrypted with the password key.
		/// </summary>
		[JsonProperty("k", Required = Required.Always)]
		public Base64Data MasterKey { get; set; }

		/// <summary>
		/// Array of MPIs encrypted with the master key.
		/// </summary>
		[JsonProperty("privk", Required = Required.Always)]
		public Base64Data PrivateKeyComponents { get; set; }
	}
}