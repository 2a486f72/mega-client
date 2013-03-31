namespace Mega.Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Mega.Client;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// Loads the configuration data for test accounts and enables you to bring them into a known state.
	/// </summary>
	/// <remarks>
	/// Two test accounts are needed. Both must be existing and verified Mega accounts.
	/// The first account is the primary one - used by most tests. The second one is used for testing multi-account operations.
	/// 
	/// You must specify the accounts in a JSON document MegaAccounts.json stored in your Documents folder. File format:
	/// {
	///		"Email1" : "something@example.com",
	///		"Password1" : "secretpassword1",
	///		"Email2" : "somethingelse@example.com",
	///		"Password2" : "secretpassword2"
	/// }
	/// </remarks>
	internal sealed class TestData
	{
		public string Email1 { get; private set; }
		public string Password1 { get; private set; }

		public string Email2 { get; private set; }
		public string Password2 { get; private set; }

		#region Account state management
		/// <summary>
		/// Brings the two test accounts to the initial state.
		/// </summary>
		/// <remarks>
		/// Account 1 contents:
		/// Files
		/// - Folder1
		/// -- Folder2
		/// -- SmallFile
		/// - BigFile
		/// - MediumFile
		/// 
		/// Account 2 contents:
		/// (empty)
		/// </remarks>
		public async Task BringToInitialState(IFeedbackChannel feedback)
		{
			var client1 = new MegaClient(Email1, Password1);

			var filesystem1 = await client1.GetFilesystemSnapshotAsync(feedback);

			// Got folders?
			var folder1 = filesystem1.Files.Children
				.FirstOrDefault(ci => ci.Type == ItemType.Folder && ci.Name == "Folder1");

			CloudItem folder2 = null;

			if (folder1 != null)
				folder2 = folder1.Children
					.FirstOrDefault(ci => ci.Type == ItemType.Folder && ci.Name == "Folder2");

			// Make folders, if needed.
			if (folder1 == null)
				folder1 = await filesystem1.Files.NewFolderAsync("Folder1", feedback);

			if (folder2 == null)
				folder2 = await folder1.NewFolderAsync("Folder2", feedback);

			// Got files?
			var bigFile = BigFile.TryFind(filesystem1);
			var mediumFile = MediumFile.TryFind(filesystem1);
			var smallFile = SmallFile.TryFind(filesystem1);

			// Then upload the new files.
			if (smallFile == null)
				using (var stream = OpenTestDataFile(SmallFile.Name))
					smallFile = await folder1.NewFileAsync(SmallFile.Name, stream, feedback);

			if (mediumFile == null)
				using (var stream = OpenTestDataFile(MediumFile.Name))
					mediumFile = await filesystem1.Files.NewFileAsync(MediumFile.Name, stream, feedback);

			if (bigFile == null)
				using (var stream = OpenTestDataFile(BigFile.Name))
					bigFile = await filesystem1.Files.NewFileAsync(BigFile.Name, stream, feedback);

			// Delete all items that we do not care about.
			var goodItems = new[]
			{
				folder1.ID,
				folder2.ID,
				bigFile.ID,
				mediumFile.ID,
				smallFile.ID
			};

			await DeleteUnwantedItems(filesystem1.Files, goodItems, feedback);
		}

		private async Task DeleteUnwantedItems(CloudItem rootDirectory, ICollection<OpaqueID> wantedItems, IFeedbackChannel feedback)
		{
			foreach (var ci in rootDirectory.Children)
			{
				if (!wantedItems.Contains(ci.ID))
				{
					await ci.DeleteAsync(feedback);
					continue;
				}

				// If it is a wanted item, it might not have wanted children, so delete them.
				if (ci.Type == ItemType.Folder)
					await DeleteUnwantedItems(ci, wantedItems, feedback);
			}
		}
		#endregion

		public static readonly TestFile BigFile = new TestFile("BigFile", 82875904);
		public static readonly TestFile MediumFile = new TestFile("MediumFile", 3143255);
		public static readonly TestFile SmallFile = new TestFile("SmallFile", 38);

		private const string Filename = "MegaAccounts.json";

		#region Singleton
		private static readonly object _instanceLock = new object();
		private static TestData _instance;

		public static TestData Current
		{
			get
			{
				lock (_instanceLock)
				{
					if (_instance == null)
						_instance = Load();

					return _instance;
				}
			}
		}

		private TestData()
		{
		}
		#endregion

		#region Test data
		public static Stream OpenTestDataFile(string filename)
		{
			Argument.ValidateIsNotNull(filename, "filename");

			// Different test runners put us in different subfolders, to just look for solution file at first.
			var solutionPath = GetSolutionDirectoryPath();
			var path = Path.Combine(solutionPath, "Mega.Tests\\TestData\\" + filename);

			if (!File.Exists(path))
				throw new ArgumentException("Test data file not found: " + path);

			return File.OpenRead(path);
		}

		private static string GetSolutionDirectoryPath()
		{
			var candidate = new DirectoryInfo(Environment.CurrentDirectory);

			while (candidate != null)
			{
				if (candidate.GetFiles("*.sln").Length != 0)
					return candidate.FullName;

				candidate = candidate.Parent;
			}

			throw new ContractException("Unable to find solution file anywhere in path - is the test running in some strange location?");
		}
		#endregion

		public sealed class TestFile
		{
			public string Name { get; private set; }
			public long Size { get; private set; }

			public Stream Open()
			{
				return OpenTestDataFile(Name);
			}

			/// <summary>
			/// Attempts to find the file in a filesystem snapshot. The location of the file is ignored, so not 100% but good enough.
			/// </summary>
			public CloudItem TryFind(FilesystemSnapshot filesystem)
			{
				return filesystem.AllItems
					.FirstOrDefault(ci => ci.Type == ItemType.File && ci.Name == Name && ci.Size == Size);
			}

			/// <summary>
			/// Finds the file in a filesystem snapshot. The location of the file is ignored, so not 100% but good enough.
			/// </summary>
			public CloudItem Find(FilesystemSnapshot filesystem)
			{
				var file = TryFind(filesystem);

				if (file == null)
					throw new ArgumentException("File was not found in cloud filesystem: " + Name);

				return file;
			}

			public TestFile(string name, long size)
			{
				Name = name;
				Size = size;
			}
		}

		private static TestData Load()
		{
			var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var path = Path.Combine(myDocuments, Filename);

			if (!File.Exists(path))
				throw new ContractException("You need to configure Mega test accounts in " + path);

			var instance = new TestData();

			try
			{
				var json = File.ReadAllText(path);
				var config = JObject.Parse(json);

				instance.Email1 = config.Value<string>("Email1");
				instance.Email2 = config.Value<string>("Email2");
				instance.Password1 = config.Value<string>("Password1");
				instance.Password2 = config.Value<string>("Password2");
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("The test accounts configuration could not be loaded.", ex);
			}

			instance.Validate();

			return instance;
		}

		private void Validate()
		{
			if (string.IsNullOrWhiteSpace(Email1))
				throw new ValidationException("Email1 field in test accounts configuration does not have a value.");

			if (string.IsNullOrWhiteSpace(Email2))
				throw new ValidationException("Email2 field in test accounts configuration does not have a value.");

			if (string.IsNullOrWhiteSpace(Password1))
				throw new ValidationException("Password1 field in test accounts configuration does not have a value.");

			if (string.IsNullOrWhiteSpace(Password2))
				throw new ValidationException("Password2 field in test accounts configuration does not have a value.");
		}
	}
}