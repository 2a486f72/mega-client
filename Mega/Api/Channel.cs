namespace Mega.Api
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using AsyncCoordinationPrimitives;
	using Messages;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// Represents a message transmission/receipt channel between you and Mega. The main focus of this interface
	/// is on execution of transactions. Always dispose of this object when done using it, otherwise you will leak memory.
	/// </summary>
	public sealed class Channel : IDisposable
	{
		/// <summary>
		/// Creates a new transaction on this channel. You can communicate to Mega by executign transactions which consist
		/// of various commands. Executing a transaction will provide you a result for every command in that transaction.
		/// </summary>
		public Transaction CreateTransaction(params object[] commands)
		{
			VerifyNotDisposed();

			var transaction = new Transaction(ExecuteCommands);

			foreach (var command in commands)
				transaction.AddCommand(command);

			return transaction;
		}

		/// <summary>
		/// Shortcut to quickly create and execute a transaction, given the set of commands in it.
		/// </summary>
		public Task<JArray> CreateAndExecuteTransactionAsync(params object[] commands)
		{
			VerifyNotDisposed();

			return CreateAndExecuteTransactionAsync(CancellationToken.None, commands);
		}

		/// <summary>
		/// Shortcut to quickly create and execute a transaction, given the set of commands in it. Supports cancellation.
		/// </summary>
		public Task<JArray> CreateAndExecuteTransactionAsync(CancellationToken cancellationToken, params object[] commands)
		{
			VerifyNotDisposed();

			var transaction = CreateTransaction(commands);

			return transaction.ExecuteAsync();
		}

		/// <summary>
		/// The session ID assigned to this channel. Defaults to null. If null, a session ID is not included in any transmissions.
		/// </summary>
		public Base64Data? SessionID
		{
			get { return _sessionID; }
			set
			{
				using (_lock.LockAsync().Result)
				{
					if (_sessionID == value)
						return;

					if (_sessionID != null)
						throw new InvalidOperationException("The session ID can only be set once. Do no reuse the same Channel for multiple sessions.");

					_sessionID = value;
				}
			}
		}

		/// <summary>
		/// When you provide this value, the channel will be able to process server-to-client notifications.
		/// Take this value from GetItemsResult. The channel will keep this value updated on its own after that.
		/// </summary>
		public Base64Data? IncomingSequenceReference
		{
			get { return _incomingSequenceReference; }
			set
			{
				using (_lock.LockAsync().Result)
					_incomingSequenceReference = value;
			}
		}

		/// <summary>
		/// Releases all resources held by this object.
		/// </summary>
		public void Dispose()
		{
			_receiveIncomingMessagesCancellationSource.Cancel();

			_disposed = true;
		}

		/// <summary>
		/// Raised when the Mega API sends us a command or notification of some kind.
		/// Events will be raised on an unspecified thread but will be raised synchronously to each other on that thread.
		/// </summary>
		public event EventHandler<IncomingNotificationEventArgs> IncomingNotification;

		#region Implementation details
		/// <summary>
		/// Protects all the instance members of the channel.
		/// </summary>
		private readonly AsyncLock _lock = new AsyncLock();

		/// <summary>
		/// A session-unique number that is incremented per dispatched request (but not changed when
		/// requests are repeated in response to network issues or EAGAIN)
		/// </summary>
		private uint _sequenceID;

		private Base64Data? _sessionID;
		private Base64Data? _incomingSequenceReference;

		private volatile bool _disposed;

		public Channel()
		{
			// Generate a new random sequence ID to start with.
			var sequenceIDBytes = Algorithms.GetRandomBytes(4);
			_sequenceID = BitConverter.ToUInt32(sequenceIDBytes, 0);

			Task.Factory.StartNew((Func<Task>)ReceiveIncomingMessages, _receiveIncomingMessagesCancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		private void VerifyNotDisposed()
		{
			if (_disposed)
				throw new ObjectDisposedException("Mega.Api.Channel");
		}

		#region Outgoing messages
		private const string OutgoingRootUrl = "https://g.api.mega.co.nz/cs";
		private readonly HttpClient _outgoingClient = new HttpClient();

		/// <summary>
		/// Executes a set of commands in a transaction, returning the results of those commands.
		/// This function can be called multiple times concurrently - the transactions will be executed sequentially.
		/// </summary>
		private async Task<JArray> ExecuteCommands(object[] commands, CancellationToken cancellationToken = default(CancellationToken))
		{
			VerifyNotDisposed();

			using (await AcquireLock(cancellationToken))
			{
				var qs = new QueryString();
				qs["id"] = unchecked(_sequenceID++).ToString(CultureInfo.InvariantCulture);

				if (_sessionID.HasValue)
					qs["sid"] = _sessionID.Value.ToString();

				var url = OutgoingRootUrl + qs;

				var result = await _outgoingClient.PostAsJsonAsyncCancellationSafe(url, commands, cancellationToken);

				// Error codes here are a request-level error equivalent to EAGAIN (-3).
				result.EnsureSuccessStatusCode();

				// API errors still come to us with 200 OK.
				// API errors are treated as results, e.g. for a two-command transaction, we could get back [-1, -1]

				var responseBody = await result.Content.ReadAsStringAsync();
				var results = JsonConvert.DeserializeObject<JArray>(responseBody);

				// Find any failure in the results and throw if found one.
				foreach (var r in results)
					ThrowOnFailureResult(r);

				return results;
			}
		}

		private async Task<IDisposable> AcquireLock(CancellationToken cancellationToken)
		{
			var lockHolder = await _lock.TryLockAsync(TimeSpan.Zero);

			if (lockHolder != null)
				return lockHolder;

			while (lockHolder == null)
			{
				cancellationToken.ThrowIfCancellationRequested();

				lockHolder = await _lock.TryLockAsync(TimeSpan.FromSeconds(0.1));
			}

			return lockHolder;
		}

		/// <summary>
		/// Parses the result for an executed command and throws an exception if it is a failure.
		/// </summary>
		public static void ThrowOnFailureResult(JToken result)
		{
			if (result.Type != JTokenType.Integer)
				return;

			switch ((int)result)
			{
				case 0:
					// Generic success.
					return;
				case -1:
					throw new InternalServerErrorException("Internal server error. Please report this to Mega.");
				case -2:
					throw new InvalidRequestException("There were invalid arguments in the request to Mega.");
				case -3:
					throw new TemporaryServerErrorException("Server is busy - must try again soon.");
				case -4:
					throw new RateLimitHitException("Rate limit hit. Mega thinks you are doing too much too fast.");
				case -5:
					throw new CriticalUploadFailureException("Critical upload failure occurred in Mega.");
				case -6:
					throw new TooManyConcurrentClientsException("Too many concurrent client IP addresses are accessing the service.");
				case -7:
					throw new InvalidRequestException("The chunk to be uploaded was found not to start on a chunk boundary by Mega. This might indicate a defect in the client library.");
				case -8:
					throw new UploadExpiredException("The upload took too long and has expired.");
				case -9:
					throw new ItemNotFoundException("Item not found. The filesystem was probably modified by another operator.");
				case -10:
					throw new CircularReferenceException("Circular reference detected in item tree.");
				case -11:
					throw new AccessDeniedException("Access denied.");
				case -12:
					throw new AlreadyExistsException("The item already exists.");
				case -13:
					throw new IncompleteItemException("The item is not yet complete, so you cannot access it.");
					// -14 is crypto failure (not returned by API, just in the list for fun)
				case -15:
					throw new SessionExpiredException("Your session has expired.");
				case -16:
					throw new BanhammerHasFallenException("You have been blocked by Mega.");
				case -17:
					throw new QuotaExceededException("You have exceeded your usage quota.");
				case -18:
					throw new ItemTemporarilyUnavailableException("The item is temporarily unavailable.");
				default:
					throw new MegaException((string)result);
			}
		}
		#endregion

		#region Incoming messages
		private const string IncomingRootUrl = "https://g.api.mega.co.nz/sc";
		private readonly HttpClient _incomingClient = new HttpClient();

		private readonly CancellationTokenSource _receiveIncomingMessagesCancellationSource = new CancellationTokenSource();

		/// <summary>
		/// Receives and publishes server-to-client notifications.
		/// </summary>
		private async Task ReceiveIncomingMessages()
		{
			while (true)
			{
				await Task.Delay(1000);

				if (_receiveIncomingMessagesCancellationSource.Token.IsCancellationRequested || _disposed)
					return;

				string url;
				using (await _lock.LockAsync())
					url = TryDetermineIncomingMessageUrl();

				if (url == null)
					continue; // We are not yet ready to receive incoming messages.

				try
				{
					var response = await _incomingClient.PostAsyncCancellationSafe(url, new StringContent(""), _receiveIncomingMessagesCancellationSource.Token);
					var messageWrapper = await response.Content.ReadAsAsync<IncomingNotificationWrapper>();

					if (messageWrapper.Notifications != null)
						foreach (var notification in messageWrapper.Notifications)
							HandleIncomingNotification(notification);

					if (messageWrapper.NextSequenceReference.HasValue)
						IncomingSequenceReference = messageWrapper.NextSequenceReference;

					if (messageWrapper.WaitUrl != null)
						await _incomingClient.PostAsyncCancellationSafe(messageWrapper.WaitUrl, new StringContent(""), _receiveIncomingMessagesCancellationSource.Token);
				}
				catch (OperationCanceledException)
				{
					// Task has been cancelled, probably because the channel has been disposed of.
					return;
				}
				catch (Exception ex)
				{
					// Well that was unexpected. Just ignore it.
					Debug.WriteLine(ex.ToString(), "ReceiveIncomingMessages");
				}
			}
		}

		private string TryDetermineIncomingMessageUrl()
		{
			if (_incomingSequenceReference == null || _sessionID == null)
				return null;

			var qs = new QueryString();
			qs["sn"] = _incomingSequenceReference.ToString();
			qs["sid"] = _sessionID.Value.ToString();

			return IncomingRootUrl + qs;
		}

		private void HandleIncomingNotification(JObject notification)
		{
			try
			{
				var args = new IncomingNotificationEventArgs(notification);

				#region Raise IncomingNotification(this, args)
				{
					var eventHandler = IncomingNotification;
					if (eventHandler != null)
						eventHandler(this, args);
				}
				#endregion

				if (!args.Handled)
					Debug.WriteLine("Unhandled notification: " + notification, "HandleIncomingNotification");
			}
			catch (Exception ex)
			{
				// Well that was unexpected. Just ignore it.
				Debug.WriteLine(ex.ToString(), "HandleIncomingNotification");
			}
		}
		#endregion

		#endregion
	}
}