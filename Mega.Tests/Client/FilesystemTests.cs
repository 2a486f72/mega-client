namespace Mega.Tests.Client
{
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
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
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.Find(filesystem);

				var newName = Algorithms.Base64Encode(Algorithms.GetRandomBytes(10));

				Debug.WriteLine("{0} -> {1}", file.Name, newName);

				file.Name = newName;

				await file.SaveAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var newFile = filesystem.AllItems
					.SingleOrDefault(ci => ci.Name == newName);

				Assert.IsNotNull(newFile);
			}
		}

		[TestMethod]
		public async Task DeletingFile_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.Find(filesystem);

				await file.DeleteAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				file = TestData.SmallFile.TryFind(filesystem);

				Assert.IsNull(file);
			}
		}

		[TestMethod]
		public async Task DeletingFileTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.Find(filesystem);

				await file.DeleteAsync(feedback);
				await file.DeleteAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				file = TestData.SmallFile.TryFind(filesystem);

				Assert.IsNull(file);
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
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var name = Algorithms.Base64Encode(TestHelper.GetRandomBytes(10));
				var folder = await filesystem.Files.NewFolderAsync(name, feedback);

				Assert.IsNotNull(folder);
				Assert.AreEqual(name, folder.Name);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.SingleOrDefault(ci => ci.Name == name);

				Assert.IsNotNull(folder);
			}
		}

		[TestMethod]
		public async Task DeletingEmptyFolder_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var folder = filesystem.AllItems
					.Single(ci => ci.Name == "Folder2" && ci.Type == ItemType.Folder);

				Assert.IsNotNull(folder);

				await folder.DeleteAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.SingleOrDefault(ci => ci.Name == "Folder2" && ci.Type == ItemType.Folder);

				Assert.IsNull(folder);
			}
		}

		[TestMethod]
		public async Task DeletingEmptyFolderTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var folder = filesystem.AllItems
					.Single(ci => ci.Name == "Folder2" && ci.Type == ItemType.Folder);

				Assert.IsNotNull(folder);

				await folder.DeleteAsync(feedback);
				await folder.DeleteAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.SingleOrDefault(ci => ci.Name == "Folder2" && ci.Type == ItemType.Folder);

				Assert.IsNull(folder);
			}
		}

		[TestMethod]
		public async Task DeletingFolderWithContents_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var folder = filesystem.AllItems
					.Single(ci => ci.Name == "Folder1" && ci.Type == ItemType.Folder);

				await folder.DeleteAsync(feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.SingleOrDefault(ci => ci.Name == "Folder1" && ci.Type == ItemType.Folder);

				Assert.IsNull(folder);
			}
		}

		[TestMethod]
		public async Task MovingFile_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.Find(filesystem);

				await file.MoveAsync(filesystem.Trash, feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				file = TestData.SmallFile.Find(filesystem);

				Assert.AreEqual(filesystem.Trash, file.Parent);
			}
		}

		[TestMethod]
		public async Task MovingFileTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var file = TestData.SmallFile.Find(filesystem);

				await file.MoveAsync(filesystem.Trash, feedback);
				await file.MoveAsync(filesystem.Trash, feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				file = TestData.SmallFile.Find(filesystem);

				Assert.AreEqual(filesystem.Trash, file.Parent);
			}
		}

		[TestMethod]
		public async Task MovingFolder_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var folder = filesystem.AllItems
					.Single(ci => ci.Type == ItemType.Folder && ci.Name == "Folder2");

				await folder.MoveAsync(filesystem.Trash, feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.Single(ci => ci.Type == ItemType.Folder && ci.Name == "Folder2");

				Assert.AreEqual(filesystem.Trash, folder.Parent);
			}
		}

		[TestMethod]
		public async Task MovingFolderTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				var folder = filesystem.AllItems
					.Single(ci => ci.Type == ItemType.Folder && ci.Name == "Folder2");

				await folder.MoveAsync(filesystem.Trash, feedback);
				await folder.MoveAsync(filesystem.Trash, feedback);

				filesystem = await client.GetFilesystemSnapshotAsync(feedback);
				folder = filesystem.AllItems
					.Single(ci => ci.Type == ItemType.Folder && ci.Name == "Folder2");

				Assert.AreEqual(filesystem.Trash, folder.Parent);
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

				// The file is uploaded just fine but when you load the result, it has -1 size. Weird. Whatever.
				// We throw an exception when trying to initialize such a CloudItem, so this should throw.
				await snapshot.Files.NewFileAsync(filename, new MemoryStream(0), feedback);
			}
		}
	}
}