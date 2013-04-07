namespace Mega.Tests.Client
{
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class SanityTests
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
				await client.GetContactListSnapshotAsync(feedback);
			}
		}

		[TestMethod]
		public async Task FilesystemLoad_Pure_DoesNotCauseApocalypse()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);
				await client.EnsureConnectedAsync(feedback);
				var filesystem = await client.GetFilesystemSnapshotAsync(feedback);

				foreach (var item in filesystem.AllItems)
				{
					feedback.WriteVerbose("Got {0}, ID_{1}, {2}", item.Name, item.ID, item.Type);
				}
			}
		}
	}
}