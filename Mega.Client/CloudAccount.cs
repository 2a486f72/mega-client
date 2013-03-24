namespace Mega.Client
{
	using Api.Messages;
	using Useful;

	public sealed class CloudAccount
	{
		public string Email { get; private set; }
		public OpaqueID ID { get; private set; }

		/// <summary>
		/// Materializes an instance from an Account structure returned by the Mega API, treated as a template for this account.
		/// </summary>
		internal static CloudAccount FromTemplate(Account template, MegaClient client)
		{
			Argument.ValidateIsNotNull(template, "template");
			Argument.ValidateIsNotNull(client, "client");

			var account = new CloudAccount(client)
			{
				ID = template.AccountID,
				Email = template.Email
			};

			return account;
		}

		private CloudAccount(MegaClient client)
		{
			_client = client;
		}

		private readonly MegaClient _client;
	}
}