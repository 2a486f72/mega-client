namespace Mega.Tests.Client
{
	using System.Linq;
	using System.Threading.Tasks;
	using Mega.Client;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class ContactListTests
	{
		[TestMethod]
		public async Task AddingContact_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);

				await client.AddContactAsync(TestData.Current.Email2, feedback);

				var contactList = await client.GetContactListSnapshotAsync(feedback);

				Assert.AreEqual(1, contactList.Count);
				Assert.AreEqual(TestData.Current.Email2, contactList.Single().Email);
			}
		}

		[TestMethod]
		public async Task RemovingContact_SeemsToWork()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email2, TestData.Current.Password2);

				var contactList = await client.GetContactListSnapshotAsync(feedback);
				Assert.AreEqual(1, contactList.Count);

				await contactList.Single().RemoveAsync(feedback);

				contactList = await client.GetContactListSnapshotAsync(feedback);
				Assert.AreEqual(0, contactList.Count);
			}
		}

		[TestMethod]
		public async Task AddingContactTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email1, TestData.Current.Password1);

				await client.AddContactAsync(TestData.Current.Email2, feedback);
				await client.AddContactAsync(TestData.Current.Email2, feedback);

				var contactList = await client.GetContactListSnapshotAsync(feedback);

				Assert.AreEqual(1, contactList.Count);
				Assert.AreEqual(TestData.Current.Email2, contactList.Single().Email);
			}
		}

		[TestMethod]
		public async Task RemovingContactTwice_DoesNoHarm()
		{
			using (var feedback = new DebugFeedbackChannel("Test"))
			{
				using (var initializing = feedback.BeginSubOperation("InitializeData"))
					await TestData.Current.BringToInitialState(initializing);

				var client = new MegaClient(TestData.Current.Email2, TestData.Current.Password2);

				var contactList = await client.GetContactListSnapshotAsync(feedback);
				Assert.AreEqual(1, contactList.Count);

				await contactList.Single().RemoveAsync(feedback);
				await contactList.Single().RemoveAsync(feedback);

				contactList = await client.GetContactListSnapshotAsync(feedback);
				Assert.AreEqual(0, contactList.Count);
			}
		}
	}
}