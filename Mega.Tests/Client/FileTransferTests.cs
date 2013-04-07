namespace Mega.Tests.Client
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class FileTransferTests
	{
		/// <summary>
		/// Downloads the specified test file and verifies its contents.
		/// </summary>
		private async Task TestFileDownload(TestData.TestFile testFile)
		{
			var target = Path.GetTempFileName();

			try
			{
				Debug.WriteLine("Temporary local file: " + target);

				using (var feedback = new DebugFeedbackChannel("Test"))
				{
					using (var initializing = feedback.BeginSubOperation("InitializeData"))
						await TestData.Current.BringToInitialState(initializing);

					var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
					var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

					var file = testFile.TryFind(filesystem);

					if (file == null)
						Assert.Fail("Could not find expected file to download: " + testFile.Name);

					await file.DownloadContentsAsync(target, feedback);

					using (var expectedContents = testFile.Open())
					using (var contents = File.OpenRead(target))
						TestHelper.AssertStreamsAreEqual(expectedContents, contents);
				}
			}
			finally
			{
				File.Delete(target);
			}
		}

		[TestMethod]
		public async Task DownloadingEmptyFile_SeemsToWork()
		{
			await TestFileDownload(TestData.EmptyFile);
		}

		[TestMethod]
		public async Task DownloadingSmallFile_SeemsToWork()
		{
			await TestFileDownload(TestData.SmallFile);
		}

		[TestMethod]
		public async Task DownloadingMediumFile_SeemsToWork()
		{
			await TestFileDownload(TestData.MediumFile);
		}

		[TestMethod]
		[TestCategory("Slow")]
		public async Task DownloadingBigFile_SeemsToWork()
		{
			await TestFileDownload(TestData.BigFile);
		}

		/// <summary>
		/// Uploads the specified test file and ensures that it looks like it really did get uploaded.
		/// </summary>
		private async Task TestFileUpload(TestData.TestFile testFile)
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				// Just stick it in the root, do not care about what already exists there.
				CloudItem file;
				using (var stream = testFile.Open())
					file = await filesystem.Files.NewFileAsync(testFile.Name, stream, feedback);

				Assert.AreEqual(testFile.Name, file.Name);
				Assert.AreEqual(testFile.Size, file.Size);
			}
		}

		[TestMethod]
		public async Task UploadingSmallFile_SeemsToWork()
		{
			await TestFileUpload(TestData.SmallFile);
		}

		[TestMethod]
		public async Task UploadingMediumFile_SeemsToWork()
		{
			await TestFileUpload(TestData.MediumFile);
		}

		[TestMethod]
		[TestCategory("Slow")]
		public async Task UploadingBigFile_SeemsToWork()
		{
			await TestFileUpload(TestData.BigFile);
		}

		/// <summary>
		/// Uploads and downloads random bytes of the specified length and verifies content integrity.
		/// Useful to ensure that we do not freak out with some special size due to math errors.
		/// </summary>
		private async Task TestFileUploadAndDownload(int size, FilesystemSnapshot filesystem, IFeedbackChannel feedback)
		{
			var data = TestHelper.GetRandomBytes(size);

			// Just stick it in the root, do not care about what already exists there.
			CloudItem file;
			using (var stream = new MemoryStream(data))
				file = await filesystem.Files.NewFileAsync(size.ToString(), stream, feedback);

			var target = Path.GetTempFileName();

			try
			{
				await file.DownloadContentsAsync(target, feedback);

				using (var contents = File.OpenRead(target))
				using (var expectedContents = new MemoryStream(data))
					TestHelper.AssertStreamsAreEqual(expectedContents, contents);
			}
			finally
			{
				File.Delete(target);
			}
		}

		[TestMethod]
		[TestCategory("Slow")]
		public async Task TransferringFiles_OfVariousSizes_SeemsToWork()
		{
			var interestingSizes = new List<int>
			{
				0,
				1,
				8,
				15,
				16,
				255,
				256,
				1023,
				1024,
			};

			// Now also add all the chunk sizes and chunk +-1 sizes for a reasonable amount of chunks.
			var chunkSizes = Algorithms.MeasureChunks(8 * 1024 * 1024);

			foreach (var chunkSize in chunkSizes)
			{
				interestingSizes.Add(chunkSize - 16);
				interestingSizes.Add(chunkSize - 15);
				interestingSizes.Add(chunkSize - 1);
				interestingSizes.Add(chunkSize);
				interestingSizes.Add(chunkSize + 1);
				interestingSizes.Add(chunkSize + 15);
				interestingSizes.Add(chunkSize + 16);
			}

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				foreach (var size in interestingSizes)
					await TestFileUploadAndDownload(size, filesystem, feedback);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(OperationCanceledException))]
		public async Task CancelingDownload_SeemsToWork()
		{
			var target = Path.GetTempFileName();

			try
			{
				Debug.WriteLine("Temporary local file: " + target);

				using (var feedback = new DebugFeedbackChannel("Test"))
				{
					using (var initializing = feedback.BeginSubOperation("InitializeData"))
						await TestData.Current.BringToInitialState(initializing);

					var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
					var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

					var file = TestData.BigFile.TryFind(filesystem);

					if (file == null)
						Assert.Fail("Could not find the test file to download.");

					CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

					await file.DownloadContentsAsync(target, feedback, cts.Token);
				}
			}
			finally
			{
				File.Delete(target);
			}
		}
	}
}