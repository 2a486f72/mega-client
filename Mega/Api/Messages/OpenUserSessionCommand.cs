namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	/// <summary>
	/// Opens a new user session. We first have to prove to the server that we might be who we claim to be, by providing
	/// a hash that we can only calculate if we know the password.
	/// </summary>
	public sealed class OpenUserSessionCommand
	{
		[JsonProperty("a")]
		public readonly string CommandName = "us";

		/// <summary>
		/// Your e-mail address as plain text.
		/// </summary>
		[JsonProperty("user")]
		public string Email { get; set; }

		/// <summary>
		/// Proves to Mega that we are allowed to access this user account.
		/// Formula: Stringhash(Email.ToLower(), PasswordAesKey).
		/// </summary>
		[JsonProperty("uh")]
		public Base64Data EmailHash { get; set; }
	}
}