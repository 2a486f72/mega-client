namespace Mega.Tests.Client
{
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class FilesystemTests
	{
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