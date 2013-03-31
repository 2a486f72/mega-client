namespace Mega.Client
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Runtime.ExceptionServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Api;
	using Api.Messages;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// An item in the cloud filesystem, be it a file, folder or anything else.
	/// </summary>
	public sealed class CloudItem
	{
		#region Read-only data
		public OpaqueID ID { get; private set; }
		public OpaqueID? ParentID { get; private set; }
		public OpaqueID OwnerID { get; private set; }

		public CloudItem Parent { get; internal set; }

		public ItemType Type { get; private set; }

		/// <summary>
		/// Numeric version of Type, as originally provided to us by Mega.
		/// For known values, see constants defined in KnownNodeTypes.
		/// </summary>
		public int TypeID { get; private set; }

		public long? Size { get; private set; }

		public DateTimeOffset LastUpdated { get; private set; }

		public IImmutableSet<EncryptedItemKey> EncryptedKeys { get; private set; }

		public IImmutableSet<CloudItem> Children { get; internal set; }

		/// <summary>
		/// Gets whether this item can contain child items.
		/// </summary>
		public bool IsContainer
		{
			get { return Type != ItemType.File; }
		}

		public EncryptedItemKey MyEncryptedItemKey
		{
			get { return EncryptedKeys.FirstOrDefault(k => k.SourceID == _client.AccountID); }
		}
		#endregion

		#region Modifiable data
		/// <summary>
		/// Gets or sets the name of the item.
		/// </summary>
		public string Name
		{
			get
			{
				if (Type == ItemType.Files)
					return "Files";
				else if (Type == ItemType.Trash)
					return "Trash";
				else if (Type == ItemType.Inbox)
					return "Inbox";

				if (Attributes != null && Attributes.ContainsKey("n"))
				{
					string name;
					if (Attributes.TryGetValue("n", out name))
						return name;
				}

				return string.Format("ID_{0}", ID);
			}
			set
			{
				if (Name == value)
					return;

				if (Type != ItemType.Folder && Type != ItemType.File)
					throw new InvalidOperationException("You cannot rename a built-in filesystem item.");

				Attributes["n"] = value;
			}
		}

		/// <summary>
		/// Gets the set of attributes specified on the item.
		/// Contents of the dictionary are ignored for special items, e.g. root folders.
		/// </summary>
		public ItemAttributes Attributes { get; private set; }
		#endregion

		#region Download
		/// <summary>
		/// Downloads the contents of the item to the local filesystem. This operation is only valid for files.
		/// </summary>
		/// <param name="destinationPath">Path to the destination file that will contain the contents of this item.</param>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task DownloadContentsAsync(string destinationPath, IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			Argument.ValidateIsNotNullOrWhitespace(destinationPath, "destinationPath");

			if (Type != ItemType.File)
				throw new InvalidOperationException("You can only download files.");

			PatternHelper.LogMethodCall("DownloadContentsAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (var file = File.Create(destinationPath))
			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				feedbackChannel.Status = "Requesting download URL";

				var result = await _client.ExecuteCommandInternalAsync<GetDownloadLinkResult>(feedbackChannel, cancellationToken, new GetDownloadCommand
				{
					ItemID = ID
				});

				feedbackChannel.Status = "Preparing for download";

				// We also protect against out of sync IsAvailable.
				var itemKey = _client.TryDecryptItemKey(EncryptedKeys);

				if (itemKey == null)
					throw new InvalidOperationException("The contents of this file are not available because you do not have a key for it.");

				var dataKey = Algorithms.DeriveNodeDataKey(itemKey);
				var nonce = itemKey.Skip(16).Take(8).ToArray();
				var metaMac = itemKey.Skip(24).Take(8).ToArray();

				feedbackChannel.Status = "Pre-allocating file";

				file.SetLength(result.Size);

				feedbackChannel.Status = "Downloading";

				var chunkSizes = Algorithms.MeasureChunks(result.Size);
				var chunkCount = chunkSizes.Length;

				var chunkMacs = new byte[chunkCount][];

				// Limit number of chunks in flight at the same time.
				var concurrentDownloadSemaphore = new SemaphoreSlim(4);

				// Only one file write operation can take place at a time.
				var concurrentWriteSemaphore = new SemaphoreSlim(1);

				// For progress calculations.
				long completedBytes = 0;

				CancellationTokenSource chunkDownloadsCancellationSource = new CancellationTokenSource();

				// Get chunks in parallel.
				List<Task> chunkDownloads = new List<Task>();
				for (int i = 0; i < chunkCount; i++)
				{
					int chunkIndex = i;
					long startOffset = chunkSizes.Take(i).Select(size => (long)size).Sum();
					long endOffset = startOffset + chunkSizes[i];

					// Each chunk is downloaded and processed by this separately.
					chunkDownloads.Add(Task.Run(async delegate
					{
						var operationName = string.Format("Downloading chunk {0} of {1}", chunkIndex + 1, chunkSizes.Length);
						using (var chunkFeedbackChannel = feedbackChannel.BeginSubOperation(operationName))
						{
							byte[] bytes = null;

							using (await SemaphoreLock.TakeAsync(concurrentDownloadSemaphore))
							{
								await RetryHelper.ExecuteWithRetryAsync(async delegate
								{
									chunkDownloadsCancellationSource.Token.ThrowIfCancellationRequested();

									chunkFeedbackChannel.Status = string.Format("Downloading {0} bytes", chunkSizes[chunkIndex]);

									// Range is inclusive, so do -1 for the end offset.
									var url = result.DownloadUrl + "/" + startOffset + "-" + (endOffset - 1);

									HttpResponseMessage response;
									using (var client = new HttpClient())
										response = await client.GetAsyncCancellationSafe(url, chunkDownloadsCancellationSource.Token);

									response.EnsureSuccessStatusCode();

									bytes = await response.Content.ReadAsByteArrayAsync();

									if (bytes.Length != chunkSizes[chunkIndex])
										throw new MegaException(string.Format("Expected {0} bytes in chunk but got {1}.", chunkSizes[chunkIndex], bytes.Length));
								}, ChunkDownloadRetryPolicy, chunkFeedbackChannel, chunkDownloadsCancellationSource.Token);
							}

							chunkDownloadsCancellationSource.Token.ThrowIfCancellationRequested();

							// OK, got the bytes. Now decrypt them and calculate MAC.
							chunkFeedbackChannel.Status = "Decrypting";

							byte[] chunkMac;
							Algorithms.DecryptNodeDataChunk(bytes, dataKey, nonce, out chunkMac, startOffset);
							chunkMacs[chunkIndex] = chunkMac;

							chunkDownloadsCancellationSource.Token.ThrowIfCancellationRequested();

							// Now write to file.
							chunkFeedbackChannel.Status = "Writing to file";

							using (await SemaphoreLock.TakeAsync(concurrentWriteSemaphore))
							{
								file.Position = startOffset;
								file.Write(bytes, 0, bytes.Length);
								file.Flush(true);
							}

							Interlocked.Add(ref completedBytes, chunkSizes[chunkIndex]);
						}
					}, chunkDownloadsCancellationSource.Token));
				}

				// Wait for all tasks to finish. Stop immediately on cancel or if any single task fails.
				while (chunkDownloads.Any(d => !d.IsCompleted))
				{
					feedbackChannel.Progress = Interlocked.Read(ref completedBytes) * 1.0 / result.Size;

					Exception failureReason = null;

					if (cancellationToken.IsCancellationRequested)
					{
						failureReason = new OperationCanceledException();
					}
					else
					{
						var failedTask = chunkDownloads.FirstOrDefault(d => d.IsFaulted || d.IsCanceled);

						if (failedTask != null)
						{
							if (failedTask.Exception != null)
								failureReason = failedTask.Exception.GetBaseException();
							else
								failureReason = new MegaException("The file could not be downloaded.");
						}
					}

					if (failureReason == null)
					{
						await Task.Delay(1000);
						continue;
					}

					chunkDownloadsCancellationSource.Cancel();

					feedbackChannel.Status = "Stopping download due to subtask failure";

					try
					{
						// Wait for all of the tasks to complete, just so we do not leave any dangling activities in the background.
						Task.WaitAll(chunkDownloads.ToArray());
					}
					catch
					{
						// It will throw something no notify us of the cancellation. Whatever, do not care.
					}

					// Rethrow the failure causing exception.
					ExceptionDispatchInfo.Capture(failureReason).Throw();
				}

				feedbackChannel.Progress = 1;

				feedbackChannel.Status = "Verifying file";

				// Verify meta-MAC.
				byte[] calculatedMetaMac = Algorithms.CalculateMetaMac(chunkMacs, dataKey);

				if (!metaMac.SequenceEqual(calculatedMetaMac))
					throw new DataIntegrityException("File meta-MAC did not match expected value. File may have been corrupted during download.");
			}
		}
		#endregion

		#region Upload/new
		/// <summary>
		/// Creates a new folder in the current folder. This operation is only valid for folders.
		/// The folder is not added to any existing filesystem snapshot, but you can use the returned object to operate on it.
		/// </summary>
		/// <param name="name">The name of the folder.</param>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		/// <returns></returns>
		public async Task<CloudItem> NewFolderAsync(string name, IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			Argument.ValidateIsNotNullOrWhitespace(name, "name");

			if (name.IndexOfAny(new[] { '/', '\\' }) != -1)
				throw new ArgumentException("A folder name cannot contain path separator characters.", "name");

			if (!IsContainer)
				throw new InvalidOperationException("This item cannot contain child items.");

			PatternHelper.LogMethodCall("NewFolderAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				feedbackChannel.Status = "Creating folder";

				var itemKey = Algorithms.GetRandomBytes(16);
				var attributesKey = Algorithms.DeriveNodeAttributesKey(itemKey);

				var result = await _client.ExecuteCommandInternalAsync<NewItemsResult>(feedbackChannel, cancellationToken, new NewItemsCommand
				{
					ClientInstanceID = _client._clientInstanceID,
					ParentID = ID,
					Items = new[]
					{
						new NewItemsCommand.NewItem
						{
							Attributes = new ItemAttributes
							{
								{ "n", name }
							}.SerializeAndEncrypt(attributesKey),
							Type = KnownItemTypes.Folder,
							EncryptedItemKey = Algorithms.EncryptKey(itemKey, _client._masterKey)
						}
					}
				});

				_client.InvalidateFilesystemInternal();

				return FromTemplate(result.Items.Single(), _client);
			}
		}

		/// <summary>
		/// Creates a new file in the current folder. This operation is only valid for folders.
		/// The file is not added to any existing filesystem snapshot, but you can use the returned object to operate on it.
		/// </summary>
		/// <param name="name">The name of the file to add.</param>
		/// <param name="contents">A stream with the contents that the file will have.</param>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task<CloudItem> NewFileAsync(string name, Stream contents, IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			Argument.ValidateIsNotNullOrWhitespace(name, "name");
			Argument.ValidateIsNotNull(contents, "contents");

			if (name.IndexOfAny(new[] { '/', '\\' }) != -1)
				throw new ArgumentException("A file name cannot contain path separator characters.", "name");

			if (!IsContainer)
				throw new InvalidOperationException("This item cannot contain child items.");

			if (!contents.CanRead)
				throw new ArgumentException("The contents stream is not readable.", "contents");

			if (!contents.CanSeek)
				throw new ArgumentException("The contents stream is not seekable.", "contents");

			PatternHelper.LogMethodCall("NewFileAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				feedbackChannel.Status = "Preparing for upload";

				var beginUploadResult = await _client.ExecuteCommandInternalAsync<BeginUploadResult>(feedbackChannel, cancellationToken, new BeginUploadCommand
				{
					Size = contents.Length
				});

				cancellationToken.ThrowIfCancellationRequested();

				feedbackChannel.Status = "Uploading file contents";

				Base64Data? completionToken = null; // Set when last chunk has been uploaded.

				var chunkSizes = Algorithms.MeasureChunks(contents.Length);
				var chunkCount = chunkSizes.Length;

				var chunkMacs = new byte[chunkCount][];

				var dataKey = Algorithms.GetRandomBytes(16);
				var nonce = Algorithms.GetRandomBytes(8);

				// Limit number of chunks in flight at the same time.
				var concurrentUploadSemaphore = new SemaphoreSlim(4);

				// Only one file read operation can take place at a time.
				var concurrentReadSemaphore = new SemaphoreSlim(1);

				// For progress calculations.
				long completedBytes = 0;

				CancellationTokenSource chunkUploadsCancellationSource = new CancellationTokenSource();

				var uploadTasks = new List<Task>();

				for (int i = 0; i < chunkCount; i++)
				{
					int chunkIndex = i;
					long startOffset = chunkSizes.Take(i).Select(size => (long)size).Sum();

					uploadTasks.Add(Task.Run(async delegate
					{
						var operationName = string.Format("Uploading chunk {0} of {1}", chunkIndex + 1, chunkSizes.Length);
						using (var chunkFeedback = feedbackChannel.BeginSubOperation(operationName))
						{
							byte[] bytes = new byte[chunkSizes[chunkIndex]];

							using (await SemaphoreLock.TakeAsync(concurrentUploadSemaphore))
							{
								chunkUploadsCancellationSource.Token.ThrowIfCancellationRequested();

								using (await SemaphoreLock.TakeAsync(concurrentReadSemaphore))
								{
									chunkUploadsCancellationSource.Token.ThrowIfCancellationRequested();

									chunkFeedback.Status = "Reading contents";

									// Read in the raw bytes for this chunk.
									contents.Position = startOffset;
									contents.Read(bytes, 0, bytes.Length);
								}

								chunkFeedback.Status = "Encrypting contents";

								byte[] chunkMac;
								Algorithms.EncryptNodeDataChunk(bytes, dataKey, nonce, out chunkMac, startOffset);
								chunkMacs[chunkIndex] = chunkMac;

								await RetryHelper.ExecuteWithRetryAsync(async delegate
								{
									chunkUploadsCancellationSource.Token.ThrowIfCancellationRequested();

									chunkFeedback.Status = string.Format("Uploading {0} bytes", chunkSizes[chunkIndex]);

									var url = beginUploadResult.UploadUrl + "/" + startOffset;

									HttpResponseMessage response;
									using (var client = new HttpClient())
										response = await client.PostAsyncCancellationSafe(url, new ByteArrayContent(bytes), chunkUploadsCancellationSource.Token);

									response.EnsureSuccessStatusCode();

									var responseBody = await response.Content.ReadAsStringAsync();

									// Result from last chunk is: base64-encoded completion handle to give to NewItemsCommand
									// Negative ASCII integer in case of error. Standard-ish stuff?
									// Empty is just OK but not last chunk.

									if (responseBody.StartsWith("["))
									{
										// Error result!
										// Assuming it is formatted like this, I never got it to return an error result.
										// It always just hangs if I do anything funny...
										var errorResult = JObject.Parse(responseBody);

										Channel.ThrowOnFailureResult(errorResult);
										throw new ProtocolViolationException("Got an unexpected result from chunk upload: " + responseBody);
									}
									else if (!string.IsNullOrWhiteSpace(responseBody))
									{
										// Completion token!
										completionToken = responseBody;
									}

									if (bytes.Length != chunkSizes[chunkIndex])
										throw new MegaException(string.Format("Expected {0} bytes in chunk but got {1}.", chunkSizes[chunkIndex], bytes.Length));
								}, ChunkUploadRetryPolicy, chunkFeedback, chunkUploadsCancellationSource.Token);
							}

							Interlocked.Add(ref completedBytes, chunkSizes[chunkIndex]);
						}
					}, chunkUploadsCancellationSource.Token));
				}

				// Wait for all tasks to finish. Stop immediately on cancel or if any single task fails.
				while (uploadTasks.Any(d => !d.IsCompleted))
				{
					feedbackChannel.Progress = Interlocked.Read(ref completedBytes) * 1.0 / contents.Length;

					Exception failureReason = null;

					if (cancellationToken.IsCancellationRequested)
					{
						failureReason = new OperationCanceledException();
					}
					else
					{
						var failedTask = uploadTasks.FirstOrDefault(d => d.IsFaulted || d.IsCanceled);

						if (failedTask != null)
						{
							if (failedTask.Exception != null)
								failureReason = failedTask.Exception.GetBaseException();
							else
								failureReason = new MegaException("The file could not be uploaded.");
						}
					}

					if (failureReason == null)
					{
						await Task.Delay(1000);
						continue;
					}

					chunkUploadsCancellationSource.Cancel();

					feedbackChannel.Status = "Stopping upload due to subtask failure";

					try
					{
						// Wait for all of the tasks to complete, just so we do not leave any dangling activities in the background.
						Task.WaitAll(uploadTasks.ToArray());
					}
					catch
					{
						// It will throw something no notify us of the cancellation. Whatever, do not care.
					}

					// Rethrow the failure causing exception.
					ExceptionDispatchInfo.Capture(failureReason).Throw();
				}

				if (!completionToken.HasValue)
					throw new ProtocolViolationException("Mega did not provide upload completion token.");

				feedbackChannel.Progress = 1;
				feedbackChannel.Progress = null;

				feedbackChannel.Status = "Creating filesystem entry";

				var metaMac = Algorithms.CalculateMetaMac(chunkMacs, dataKey);
				var itemKey = Algorithms.CreateNodeKey(dataKey, nonce, metaMac);

				var attributesKey = Algorithms.DeriveNodeAttributesKey(itemKey);

				// Create the file from the uploaded data.
				var result = await _client.ExecuteCommandInternalAsync<NewItemsResult>(feedbackChannel, cancellationToken, new NewItemsCommand
				{
					ClientInstanceID = _client._clientInstanceID,
					ParentID = ID,
					Items = new[]
					{
						new NewItemsCommand.NewItem
						{
							Attributes = new ItemAttributes
							{
								{ "n", name }
							}.SerializeAndEncrypt(attributesKey),
							Type = KnownItemTypes.File,
							EncryptedItemKey = Algorithms.EncryptKey(itemKey, _client._masterKey),
							UploadCompletionToken = completionToken.Value
						}
					}
				});

				_client.InvalidateFilesystemInternal();

				return FromTemplate(result.Items.Single(), _client);
			}
		}
		#endregion

		#region Move
		/// <summary>
		/// Moves the item under another item. This operation is valid for files and folders.
		/// The parent is not updated in any existing filesystem snapshot.
		/// </summary>
		/// <param name="newParent">The new parent of the item.</param>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task MoveAsync(CloudItem newParent, IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			Argument.ValidateIsNotNull(newParent, "newParent");

			if (Type != ItemType.File && Type != ItemType.Folder)
				throw new InvalidOperationException("You can only move files or folders.");

			if (!newParent.IsContainer)
				throw new InvalidOperationException("The specified destination cannot contain other items.");

			PatternHelper.LogMethodCall("MoveAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				await _client.ExecuteCommandInternalAsync<SuccessResult>(feedbackChannel, cancellationToken, new MoveItemCommand
				{
					ClientInstanceID = _client._clientInstanceID,
					ItemID = ID,
					ParentID = newParent.ID
				});

				_client.InvalidateFilesystemInternal();
			}
		}
		#endregion

		#region Delete
		/// <summary>
		/// Deletes the item.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task DeleteAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (Type != ItemType.File && Type != ItemType.Folder)
				throw new InvalidOperationException("You can only delete files or folders.");

			PatternHelper.LogMethodCall("DeleteAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				feedbackChannel.Status = "Deleting item: " + Name;

				await _client.ExecuteCommandInternalAsync<SuccessResult>(feedbackChannel, cancellationToken, new DeleteItemCommand
				{
					ClientInstanceID = _client._clientInstanceID,
					ItemID = ID,
				});

				_client.InvalidateFilesystemInternal();
			}
		}
		#endregion

		#region Save
		/// <summary>
		/// Saves the name and any other attributes after they have been locally modified.
		/// </summary>
		/// <param name="feedbackChannel">Allows you to receive feedback about the operation while it is running.</param>
		/// <param name="cancellationToken">Allows you to cancel the operation.</param>
		public async Task SaveAsync(IFeedbackChannel feedbackChannel = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			PatternHelper.LogMethodCall("SaveAsync", feedbackChannel, cancellationToken);
			PatternHelper.EnsureFeedbackChannel(ref feedbackChannel);

			if (Type != ItemType.File && Type != ItemType.Folder)
				throw new InvalidOperationException("You can only modify files and folders.");

			using (await _client.AcquireLock(feedbackChannel, cancellationToken))
			{
				var itemKey = _client.TryDecryptItemKey(EncryptedKeys);
				var attributesKey = Algorithms.DeriveNodeAttributesKey(itemKey);

				await _client.ExecuteCommandInternalAsync<SuccessResult>(feedbackChannel, cancellationToken, new SetItemAttributesCommand
				{
					ClientInstanceID = _client._clientInstanceID,
					ItemID = ID,
					EncryptedItemKey = MyEncryptedItemKey.EncryptedKey,
					Attributes = Attributes.SerializeAndEncrypt(attributesKey)
				});

				_client.InvalidateFilesystemInternal();
			}
		}
		#endregion

		#region Implementation details
		/// <summary>
		/// Materializes an instance from an Item structure returned by the Mega API, treated as a template for this item.
		/// Parent-child relationships are not automatically linked up - that is left up to the creator.
		/// </summary>
		internal static CloudItem FromTemplate(Item template, MegaClient client)
		{
			Argument.ValidateIsNotNull(template, "template");
			Argument.ValidateIsNotNull(client, "client");

			CloudItem item = new CloudItem(client)
			{
				TypeID = template.Type,
				Size = template.Size,
				ID = template.ID,
				OwnerID = template.OwnerID,
				LastUpdated = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(template.Timestamp).ToLocalTime(),
				ParentID = template.ParentID,
				EncryptedKeys = template.EncryptedKeys.ToImmutableHashSet()
			};

			bool hasEncryptedData = true;

			switch (template.Type)
			{
				case KnownItemTypes.File:
					item.Type = ItemType.File;

					if (!item.Size.HasValue || item.Size < 1)
						throw new UnusableItemException("File does not have a valid size: " + item.Name);
					break;
				case KnownItemTypes.Folder:
					item.Type = ItemType.Folder;
					break;
				case KnownItemTypes.Inbox:
					item.Type = ItemType.Inbox;
					hasEncryptedData = false;
					break;
				case KnownItemTypes.Trash:
					item.Type = ItemType.Trash;
					hasEncryptedData = false;
					break;
				case KnownItemTypes.Files:
					item.Type = ItemType.Files;
					hasEncryptedData = false;
					break;
				default:
					item.Type = ItemType.Unknown;
					break;
			}

			if (hasEncryptedData)
			{
				// Decrypt the item attributes, if the item has them and if we have a key.
				var itemKey = client.TryDecryptItemKey(item.EncryptedKeys);

				if (itemKey == null)
					throw new UnusableItemException("No key for item: " + item.Name);

				// We have a key for this item!
				var attributesKey = Algorithms.DeriveNodeAttributesKey(itemKey);
				item.Attributes = ItemAttributes.DecryptAndDeserialize(template.Attributes, attributesKey);
			}

			return item;
		}

		private CloudItem(MegaClient client)
		{
			_client = client;

			Attributes = new ItemAttributes();
			Children = ImmutableHashSet.Create<CloudItem>();
			EncryptedKeys = ImmutableHashSet.Create<EncryptedItemKey>();
		}

		private readonly MegaClient _client;

		private static readonly RetryPolicy ChunkDownloadRetryPolicy = new RetryPolicy(RetryPolicy.ThirdPartyFaultExceptions, RetryPolicy.CarefulIntervals);

		private static readonly RetryPolicy ChunkUploadRetryPolicy = new RetryPolicy(RetryPolicy.ThirdPartyFaultExceptions, RetryPolicy.CarefulIntervals);
		#endregion
	}
}