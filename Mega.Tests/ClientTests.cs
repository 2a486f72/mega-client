namespace Mega.Tests
{
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
		private static class TestData1
		{
			public const string Email = "";
			public const string Password = "";
		}

		[TestMethod]
		public async Task SuccessfulLogin_IsSuccessful()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var connecting = new DebugFeedbackChannel("Test"))
				await client.EnsureConnectedAsync(connecting);
		}

		[TestMethod]
		public async Task FilesystemLoad_DoesNotCauseApocalypse()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				await client.EnsureConnectedAsync(feedback);
				await client.GetFilesystemSnapshotAsync(feedback);
			}
		}

		[TestMethod]
		public async Task AccountListLoad_DoesNotCauseApocalypse()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				await client.EnsureConnectedAsync(feedback);
				await client.GetAccountListSnapshotAsync(feedback);
			}
		}

		[TestMethod]
		public async Task DownloadingFile_SeemsToWork()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			var target = Path.GetTempFileName();
			Debug.WriteLine("Target file: " + target);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var file = snapshot.AllItems.FirstOrDefault(i => i.Type == ItemType.File && i.IsAvailable);

				if (file == null)
					Assert.Inconclusive("Could not find a file to download.");

				await file.DownloadContentsAsync(target, feedback);
			}
		}

		[TestMethod]
		public async Task DownloadingBigFile_SeemsToWork()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			var target = Path.GetTempFileName();
			Debug.WriteLine("Target file: " + target);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var file = snapshot.AllItems.Where(i => i.Type == ItemType.File && i.IsAvailable && i.Size.HasValue)
					.OrderByDescending(i => i.Size.Value).FirstOrDefault();

				if (file == null)
					Assert.Inconclusive("Could not find a file to download.");

				await file.DownloadContentsAsync(target, feedback);
			}
		}

		[TestMethod]
		public async Task RenamingFile_SeemsToWork()
		{
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

				var file = snapshot.AllItems.FirstOrDefault(i => i.Type == ItemType.File && i.IsAvailable);

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
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var feedback = new DebugFeedbackChannel("Test"))
			{
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
			var client = new MegaClient(TestData1.Email, TestData1.Password);

			using (var contents = Assembly.GetExecutingAssembly().GetManifestResourceStream("Mega.Tests.TestData.mediumsize.zip"))
			{
				using (var feedback = new DebugFeedbackChannel("Test"))
				{
					var snapshot = await client.GetFilesystemSnapshotAsync(feedback);

					var filename = Algorithms.Base64Encode(Algorithms.GetRandomBytes(10));

					var item = await snapshot.Files.NewFileAsync(filename, contents, feedback);

					Assert.IsNotNull(item);
					Assert.AreEqual(filename, item.Name);
					Assert.AreEqual(contents.Length, item.Size);
				}
			}
		}
	}
}