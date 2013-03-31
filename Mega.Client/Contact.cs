namespace Mega.Client
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Api;
	using Api.Messages;
	using Useful;

	/// <summary>
	/// An entry in your contact list.
	/// </summary>
	public sealed class Contact
	{
		public OpaqueID ID { get; private set; }
		public string Email { get; private set; }

		#region Delete
		/// <summary>
		/// Removes the account from your contact list.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task RemoveAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			PatternHelper.LogMethodCall("RemoveAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				try
				{
					await _client.ExecuteCommandInternalAsync<SuccessResult>(feedbackChannel, cancellationToken, new SetContactStatusCommand
					{
						ClientInstanceID = _client._clientInstanceID,
						AccountIDOrEmail = ID.ToString(),
						Status = KnownContactStatuses.NotInContactList
					});
				}
				catch (AlreadyExistsException)
				{
					// Thrown if the contact has already been removed. No problem - we can just ignore it.
				}

				_client.InvalidateContactListInternal();
			}
		}
		#endregion

		#region Implementation details
		/// <summary>
		/// Materializes an instance from an Account structure returned by the Mega API, treated as a template for this account.
		/// </summary>
		internal static Contact FromTemplate(Account template, MegaClient client)
		{
			Argument.ValidateIsNotNull(template, "template");
			Argument.ValidateIsNotNull(client, "client");

			if (template.AccountType != KnownAccountTypes.InContactList)
				throw new ArgumentException("A Contact can only be materialized from account list entries that are marked as contacts.", "template");

			var account = new Contact(client)
			{
				ID = template.AccountID,
				Email = template.Email
			};

			return account;
		}

		private Contact(MegaClient client)
		{
			_client = client;
		}

		private readonly MegaClient _client;
		#endregion
	}
}