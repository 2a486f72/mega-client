mega-client
===========

.NET 4.5 client library for the Mega API.

The library consists of two layers:

1. A basic API messaging component that simply talks to Mega without understanding the message contents (Mega library).
1. A high-level client that exposes Mega entities as an object model (Mega.Client library).

For almost all purposes, you will want to use the high-level client.

Features
===========

* Fully asynchronous API.
* Designed for serving both UI code and non-UI code equally well.
* Hierarchical status reporting API for tasks and sub-tasks.
* Automatic reconnect and retry.
* Cancellation support for long-running operations.

Implementation status
===========

GOOD:

* Filesystem operations.
* File upload.
* File download.

BAD:

* Share functionality.
* Contact list functionality.

NONE:

* User registration/invitation.
* Share URL creation.

Usage
===========

The automated tests give you a good idea of how this library can be used. Here is an example snippet that simply downloads the biggest file in your cloud filesystem, without reporting any status to a UI:

	[TestMethod]
	public async Task DownloadingBigFile_SeemsToWork()
	{
		var client = new MegaClient(TestData1.Email, TestData1.Password);
		
		var snapshot = await client.GetFilesystemSnapshotAsync();
		
		// Select the biggest file.
		var file = snapshot.AllItems
			.Where(i => i.Type == ItemType.File && i.IsAvailable && i.Size.HasValue)
			.OrderByDescending(i => i.Size.Value)
			.FirstOrDefault();
		
		if (file == null)
			Assert.Inconclusive("Could not find a file to download.");

		// Download the file.
		var target = Path.GetTempFileName();
		await file.DownloadContentsAsync(target);
	}

Automated tests
===========

The project comes with an automated test suite that tests both stand-alone pieces of functionality (e.g. crypto) and high-level client functionality (e.g. file upload/download).

At the moment, a manually prepared test account set is used for high-level Mega functionality testing; this account is not published in the repository. A future goal is to create a test data generation script that lets you set up your own test account, so no credentials need to be saved in the source repository.

The Mega test account credentials should be placed in text files Mega_Account.txt and Mega_Password.txt saved in the My Documents folder.