namespace Mega.Tests.Client
{
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class FileTransferTests
	{
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
	}
}