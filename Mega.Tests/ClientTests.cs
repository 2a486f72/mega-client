namespace Mega.Tests
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;
	using Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class ClientTests
	{
		[TestMethod]
		public async Task TestAccountInitialization_IsSuccessful()
		{
			using (var connecting = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = connecting.BeginSubOperation("InitializeData"))
				{
					await TestData.Current.BringToInitialState(initializing);

					var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
					await client.EnsureConnectedAsync(connecting);
				}
			}
		}

		[TestMethod]
		public async Task SuccessfulLogin_IsSuccessful()
		{
			var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);

			using (var connecting = new DebugFeedbackChannel("Test"))
				await client.EnsureConnectedAsync(connecting);
		}

		[TestMethod]
		public async Task FilesystemLoad_DoesNotCauseApocalypse()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				await client.EnsureConnectedAsync(feedback);
				await client.GetFilesystemSnapshotAsync(feedback);
			}
		}

		[TestMethod]
		public async Task AccountListLoad_DoesNotCauseApocalypse()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				await client.EnsureConnectedAsync(feedback);
				await client.GetAccountListSnapshotAsync(feedback);
			}
		}

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
					var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

					var file = testFile.TryFind(snapshot);

					if (file == null)
						Assert.Fail("Could not find expected file to download: " + testFile.Name);

					await file.DownloadContentsAsync(target, feedback);

					using (var expectedContents = testFile.Open())
					using (var contents = File.OpenRead(target))
					{
						Assert.AreEqual(expectedContents.Length, contents.Length);

						var reader = new BinaryReader(contents);
						var expectedReader = new BinaryReader(expectedContents);

						while (contents.Position != contents.Length)
							Assert.AreEqual(expectedReader.ReadByte(), reader.ReadByte());
					}
				}
			}
			finally
			{
				File.Delete(target);
			}
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
		public async Task DownloadingBigFile_SeemsToWork()
		{
			await TestFileDownload(TestData.BigFile);
		}

		[TestMethod]
		public async Task RenamingFile_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.TryFind(snapshot);

				if (file == null)
					Assert.Inconclusive("Could not find a file to rename.");

				var newName = Algorithms.Base64Encode(Algorithms.GetRandomBytes(10));

				Debug.WriteLine("{0} -> {1}", file.Name, newName);

				file.Name = newName;

				await file.SaveAsync(feedback);
			}
		}

		[TestMethod]
		public async Task FolderCreation_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var filesRoot = snapshot.Files;
				Assert.IsNotNull(filesRoot);

				var name = Algorithms.Base64Encode(TestHelper.GetRandomBytes(10));
				var folder = await filesRoot.NewFolderAsync(name, feedback);

				Assert.IsNotNull(folder);
				Assert.AreEqual(name, folder.Name);
			}
		}

		[TestMethod]
		public async Task UploadingFile_SeemsToWork()
		{
			using (var contents = new MemoryStream(TestHelper.GetRandomBytes(135511)))
			{

				using (var feedback = new DebugFeedbackChannel("Test"))
				{
					using (var initializing = feedback.BeginSubOperation("InitializeData"))
						await TestData.Current.BringToInitialState(initializing);

					var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
					var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

					var filename = Algorithms.Base64Encode(Algorithms.GetRandomBytes(10));

					var item = await snapshot.Files.NewFileAsync(filename, contents, feedback);

					Assert.IsNotNull(item);
					Assert.AreEqual(filename, item.Name);
					Assert.AreEqual(contents.Length, item.Size);
				}
			}
		}

		[TestMethod]
		[ExpectedException(typeof(UnusableItemException))]
		public async Task FileWithInvalidSize_CannotBeUsed()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var filename = "EmptyFile";

				await snapshot.Files.NewFileAsync(filename, new MemoryStream(0), feedback);
			}
		}
	}
}