namespace Mega.Client
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Api;
	using Api.Messages;
	using AsyncCoordinationPrimitives;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// A client connection to one Mega cloud filesystem account.
	/// </summary>
	/// <remarks>
	/// Connection management is performed automatically, including reconnection and retry when possible.
	/// All InternalAsync functions assume that the lock is already held by the caller.
	/// </remarks>
	public sealed class MegaClient : IDisposable
	{
		/// <summary>
		/// Gets the ID of the Mega account used by this client.
		/// </summary>
		public OpaqueID AccountID { get; set; }

		/// <summary>
		/// Gets the email address of the Mega account used by this client.
		/// </summary>
		public string AccountEmail { get; private set; }

		/// <summary>
		/// Gets a snapshot of the cloud filesystem's current state. You can use this snapshot to operate on the filesystem. The
		/// snapshot itself is not kept up to date by the client, so you should get a new snapshot after performing operations on it.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task<FilesystemSnapshot> GetFilesystemSnapshotAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			PatternHelper.LogMethodCall("GetFilesystemSnapshotAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await AcquireLock(feedbackChannel, cancellationToken))
			{
				await EnsureFilesystemAvailableAsync(feedbackChannel, cancellationToken);

				return _currentFilesystemSnapshot;
			}
		}

		/// <summary>
		/// Gets a snapshot of the account list's current state. You can use this snapshot to operate with contacts and
		/// known accounts (e.g. create shares for them).on the filesystem. The snapshot itself is not kept up to date
		/// by the client, so you should get a new snapshot after performing operations on it.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task<IImmutableSet<CloudAccount>> GetAccountListSnapshotAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			PatternHelper.LogMethodCall("GetAccountListSnapshotAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await AcquireLock(feedbackChannel, cancellationToken))
			{
				await EnsureFilesystemAvailableAsync(feedbackChannel, cancellationToken);

				return _currentAccountListSnapshot;
			}
		}

		/// <summary>
		/// Event raised when the cloud filesystem contents have been updated from the server and a new filesystem snapshot is available.
		/// </summary>
		public event EventHandler FilesystemChanged;

		/// <summary>
		/// Event raised when the known account list has been updated from the server and a new account list snapshot is available.
		/// </summary>
		public event EventHandler AccountListChanged;

		public MegaClient(string email, string password)
		{
			Argument.ValidateIsNotNullOrWhitespace(email, "email");
			Argument.ValidateIsNotNullOrWhitespace(password, "password");

			if (!email.Contains("@"))
				throw new ArgumentException("That does not look like an e-mail address.", "email");

			_clientInstanceID = OpaqueID.Random(Constants.ClientInstanceIdSizeInBytes);
			AccountEmail = email;
			_passwordKey = Algorithms.DeriveAesKey(password);
		}

		public void Dispose()
		{
			DestroySession();
		}

		/// <summary>
		/// Ensures that the client is connected. This is done automatically as needed
		/// but you can use it manually to verify that the login credentials are valid.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task EnsureConnectedAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			PatternHelper.LogMethodCall("EnsureConnectedAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await AcquireLock(feedbackChannel, cancellationToken))
			{
				await EnsureConnectedInternalAsync(feedbackChannel, cancellationToken);
			}
		}

		#region Implementation details
		private readonly byte[] _passwordKey;
		internal byte[] _masterKey;
		private Algorithms.RsaPrivateKey _privateKey;
		private Algorithms.RsaPublicKey _publicKey;

		/// <summary>
		/// Known user or share keys. These are primarily used to decrypt item keys.
		/// </summary>
		private readonly IDictionary<OpaqueID, byte[]> _masterKeys = new Dictionary<OpaqueID, byte[]>();

		/// <summary>
		/// Unique ID of this Mega client instance. This is used to determine where some change originated from.
		/// The point is, we can ignore change notifications from the server if the change notification carries our own ID.
		/// </summary>
		internal OpaqueID _clientInstanceID;

		/// <summary>
		/// The channel used for Mega I/O. A different channel is used for each session that the client spans.
		/// </summary>
		private Channel _channel;

		/// <summary>
		/// Attempts to find and decrypt an item key that is encrypted with an available master key.
		/// If none of the item keys use a master key we can access, returns null.
		/// </summary>
		/// <param name="itemKeys">Set of candidate item keys, encrypted with the master key of an account or share.</param>
		internal byte[] TryDecryptItemKey(IReadOnlyCollection<EncryptedItemKey> itemKeys)
		{
			Argument.ValidateIsNotNull(itemKeys, "itemKeys");

			var key = itemKeys.FirstOrDefault(itemKey => _masterKeys.ContainsKey(itemKey.SourceID));

			if (key == default(EncryptedItemKey))
				return null;

			var masterKey = _masterKeys[key.SourceID];

			return Algorithms.DecryptKey(key.EncryptedKey, masterKey);
		}

		#region Connection and session management
		/// <summary>
		/// Executes a set of commands on the Mega channel. This function automatically wraps special
		/// situations such as reconnecting when the session expires and retrying on temporary failure.
		/// Always use this method instead of directly accessing the Channel.
		/// </summary>
		internal async Task<TResult> ExecuteCommandInternalAsync<TResult>(IFeedbackChannel feedbackChannel, CancellationToken cancellationToken, object command)
		{
			var results = await ExecuteCommandsInternalAsync(feedbackChannel, cancellationToken, command);
			return results.Single().ToObject<TResult>();
		}

		/// <summary>
		/// Executes a set of commands on the Mega channel. This function automatically wraps special
		/// situations such as reconnecting when the session expires and retrying on temporary failure.
		/// Always use this method instead of directly accessing the Channel.
		/// </summary>
		internal Task<JArray> ExecuteCommandsInternalAsync(IFeedbackChannel feedbackChannel, CancellationToken cancellationToken, params object[] commands)
		{
			var retryPolicy = new RetryPolicy(RetryPolicy.ThirdPartyFaultExceptions.Concat(new[]
			{
				typeof(SessionExpiredException)
			}).ToArray(), RetryPolicy.FastAndAggressiveIntervals);

			using (var apiCallFeedbackChannel = feedbackChannel.BeginSubOperation("Communicating with Mega API"))
			{
				return RetryHelper.ExecuteWithRetryAsync(async delegate
				{
					await EnsureConnectedInternalAsync(feedbackChannel, cancellationToken);

					try
					{
						return await _channel.CreateAndExecuteTransactionAsync(cancellationToken, commands);
					}
					catch (SessionExpiredException)
					{
						DestroySession();

						throw;
					}
				}, retryPolicy, apiCallFeedbackChannel, cancellationToken);
			}
		}

		/// <summary>
		/// Forgets everything about the current session. A new session will be opened for the next operation.
		/// </summary>
		private void DestroySession()
		{
			if (_channel == null)
				return;

			Debug.WriteLine("DestroySession() called.");

			_currentFilesystemSnapshot = null;

			_channel.IncomingNotification -= OnIncomingNotification;
			_channel.Dispose();
			_channel = null;

			// We do not know the current state of the filesystem so it might have changed.
			Task.Run(delegate
			{
				#region Raise FilesystemChanged(this, EventArgs.Empty)
				{
					var eventHandler = FilesystemChanged;
					if (eventHandler != null)
						eventHandler(this, EventArgs.Empty);
				}
				#endregion
			});
		}

		private async Task EnsureConnectedInternalAsync(IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			if (_channel != null)
				return;

			var channel = new Channel();

			using (var connecting = feedbackChannel.BeginSubOperation("Establishing Mega session"))
			{
				connecting.Status = "Authenticating";

				var emailHash = Algorithms.Stringhash(AccountEmail.ToLowerInvariant(), _passwordKey);

				var sessionInfo = (await channel.CreateAndExecuteTransactionAsync(cancellationToken, new OpenUserSessionCommand
				{
					Email = AccountEmail,
					EmailHash = emailHash
				})).Single().ToObject<OpenUserSessionResult>();

				// Now do the cryptography we need to get the session ID and load the profile.
				connecting.Status = "Loading user profile";

				_masterKey = Algorithms.DecryptKey(sessionInfo.MasterKey, _passwordKey);

				var privateKeyComponents = Algorithms.DecryptKey(sessionInfo.PrivateKeyComponents, _masterKey);
				_privateKey = Algorithms.MpiArrayBytesToRsaPrivateKey(privateKeyComponents);

				var sessionIDEncrypted = Algorithms.MpiToBytes(sessionInfo.SessionIDData);
				var sessionIDRaw = Algorithms.RsaDecrypt(sessionIDEncrypted, _privateKey);

				// First 43 bytes is the session ID. There is more data but it seems to not be used.
				channel.SessionID = sessionIDRaw.Take(43).ToArray();

				// Now load the user profile.
				var profile = (await channel.CreateAndExecuteTransactionAsync(cancellationToken, new GetUserProfileCommand()))
					.Single().ToObject<GetUserProfileResult>();

				_publicKey = Algorithms.MpiArrayBytesToRsaPublicKey(profile.PublicKeyComponents);
				AccountID = profile.UserID;

				_masterKeys.Clear();
				_masterKeys[AccountID] = _masterKey;
			}

			channel.IncomingNotification += OnIncomingNotification;
			_channel = channel;
		}
		#endregion

		#region Server-to-client notification handling
		private void OnIncomingNotification(object sender, IncomingNotificationEventArgs e)
		{
			string notificationName = e.Command.Value<string>("a");

			if (notificationName == ItemAddedNotification.NotificationNameConst
				|| notificationName == ItemDeletedNotification.NotificationNameConst)
			{
				e.Handled = true;

				using (_lock.LockAsync().Result)
				{
					if (_currentFilesystemSnapshot == null)
						return;

					// Just reset the whole thing. Only raise FilesystemChanged once after each snapshot is acuired.
					_currentFilesystemSnapshot = null;
				}

				Task.Run(delegate
				{
					#region Raise FilesystemChanged(this, EventArgs.Empty)
					{
						var eventHandler = FilesystemChanged;
						if (eventHandler != null)
							eventHandler(this, EventArgs.Empty);
					}
					#endregion
				});
			}
			else if (notificationName == AccountUpdatedNotification.NotificationNameConst)
			{
				e.Handled = true;

				using (_lock.LockAsync().Result)
				{
					if (_currentAccountListSnapshot == null)
						return;

					// Just reset the whole thing. Only raise AccountListChanged once after the list is acuired.
					_currentAccountListSnapshot = null;
				}

				Task.Run(delegate
				{
					#region Raise AccountListChanged(this, EventArgs.Empty)
					{
						var eventHandler = AccountListChanged;
						if (eventHandler != null)
							eventHandler(this, EventArgs.Empty);
					}
					#endregion
				});
			}
		}
		#endregion

		#region Filesystem management and operations
		private FilesystemSnapshot _currentFilesystemSnapshot;
		private ImmutableHashSet<CloudAccount> _currentAccountListSnapshot;

		internal async Task EnsureFilesystemAvailableAsync(IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			if (_currentFilesystemSnapshot != null)
				return;

			using (var loadingFilesystem = feedbackChannel.BeginSubOperation("Loading cloud filesystem and known account list"))
			{
				loadingFilesystem.Status = "Loading encrypted filesystem";

				var filesystem = await ExecuteCommandInternalAsync<GetItemsResult>(loadingFilesystem, cancellationToken, new GetItemsCommand
				{
					C = 1
				});

				// This enables the channel to start change tracking.
				if (_channel.IncomingSequenceReference == null)
					_channel.IncomingSequenceReference = filesystem.IncomingSequenceReference;

				loadingFilesystem.Status = "Decrypting filesystem";

				var snapshot = CreateFilesystemSnapshot(filesystem, loadingFilesystem, cancellationToken);

				feedbackChannel.Status = "Performing integrity check";

				CheckSnapshotIntegrity(snapshot, loadingFilesystem, cancellationToken);

				feedbackChannel.Status = "Loading account list";

				_currentAccountListSnapshot = filesystem.KnownAccounts.Select(a => CloudAccount.FromTemplate(a, this)).ToImmutableHashSet();

				_currentFilesystemSnapshot = snapshot;
			}
		}

		private FilesystemSnapshot CreateFilesystemSnapshot(GetItemsResult filesystem, IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			var snapshot = new FilesystemSnapshot(this);

			long processedItems = 0;

			foreach (var nodeInfo in filesystem.Items)
			{
				try
				{
					snapshot.AddItem(nodeInfo);
				}
				catch (Exception ex)
				{
					// Ignore any items that fail to materialize.
					feedbackChannel.WriteWarning(ex.Message);
				}

				feedbackChannel.Progress = ++processedItems * 1.0 / filesystem.Items.Length;
				cancellationToken.ThrowIfCancellationRequested();
			}

			return snapshot;
		}

		private void CheckSnapshotIntegrity(FilesystemSnapshot snapshot, IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			foreach (var orphan in snapshot._orphans)
				feedbackChannel.WriteWarning("Found orphaned item: {0} (ID {1})", orphan.Name, orphan.ID);

			foreach (var item in snapshot.AllItems)
				feedbackChannel.WriteVerbose("Got item: {0} ({1})", item.Name, item.Type);
		}
		#endregion

		#region Locking and synchronization
		private readonly AsyncLock _lock = new AsyncLock();

		internal async Task<IDisposable> AcquireLock(IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			// First try - if lock is free, we just grab it immediately.
			var firstAttempt = await _lock.TryLockAsync(TimeSpan.Zero);

			if (firstAttempt != null)
				return firstAttempt;

			feedbackChannel.Status = "Waiting for pending operations to complete";

			// Wait in a loop, checking for cancellation every now and then.
			while (true)
			{
				var lockHolder = await _lock.TryLockAsync(TimeSpan.FromSeconds(0.1));

				if (lockHolder != null)
					return lockHolder;

				cancellationToken.ThrowIfCancellationRequested();
			}
		}
		#endregion

		#endregion
	}
}