mega-client
===========

.NET 4.5 client library for the Mega API.

**Available as a NuGet package: Mega**

The library consists of two layers:

1. A basic API messaging component that simply talks to Mega without understanding the message contents (`Mega` library).
1. A high-level client that exposes Mega entities as an object model (`Mega.Client` library).

For almost all purposes, you will want to use the high-level client and its `MegaClient` class.

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
* Contact list functionality.
* User invitation.
* Send files/folders to contact.

BAD:
* Share usage functionality.

NONE:

* Share management functionality.
* User registration.
* Share URL creation.

Usage
===========

The automated tests give you a good idea of how this library can be used. Here is an example snippet that simply downloads the biggest file in your cloud filesystem, without reporting any status to a UI:

```CS
	public async Task DownloadBiggestFile(string target)
	{
		var client = new MegaClient("myaccount@example.com", "MySecretPassword123");
		
		var filesystem = await client.GetFilesystemSnapshotAsync();
		
		// Select the biggest file in the entire filesystem, no matter where it is located in the tree.
		var file = filesystem.AllItems
			.Where(i => i.Type == ItemType.File)
			.OrderByDescending(i => i.Size.Value)
			.FirstOrDefault();
		
		if (file == null)
			throw new Exception("There are no files in your account.");

		await file.DownloadContentsAsync(target);
	}
```

Automated tests
===========

The project comes with an automated test suite that tests both stand-alone pieces of functionality (e.g. crypto) and high-level client functionality (e.g. file upload/download).

The test data is included and tests accounts are automatically populated with the relevant data. All you need to do is to create a file called MegaAccounts.json in your Documents folder. This file should include credentials for two Mega accounts to use for testing. The file format is shown below.

	{
		"Email1" : "megatest1@example.com",
		"Password1" : "password1",
		"Email2" : "megatest2@example.com",
		"Password2" : "password2"
	}
