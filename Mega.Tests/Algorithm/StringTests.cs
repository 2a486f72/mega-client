namespace Mega.Tests.Algorithm
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class StringTests
	{
		[TestMethod]
		public void StringToPaddedBytes_SeemsToWork()
		{
			string testData = "Hello";

			byte[] expectedBytes = new byte[]
			{
				(byte)'H',
				(byte)'e',
				(byte)'l',
				(byte)'l',
				(byte)'o',
				0,
				0,
				0
			};

			var bytes = Algorithms.StringToMangledAndPaddedBytes(testData);

			CollectionAssert.AreEqual(expectedBytes, bytes);
		}

		[TestMethod]
		public void StringToPaddedBytes_WithVariousLengths_Works()
		{
			for (int length = 0; length < 100; length++)
			{
				string testData = new string('a', length);

				var expectedLength = ((length + 3) / 4) * 4;

				var bytes = Algorithms.StringToMangledAndPaddedBytes(testData);

				Assert.AreEqual(expectedLength, bytes.Length);

				for (int i = 0; i < bytes.Length; i++)
				{
					byte expectedValue;

					if (i < length)
						expectedValue = (byte)'a';
					else
						expectedValue = 0;

					Assert.AreEqual(expectedValue, bytes[i]);
				}
			}
		}

		[TestMethod]
		public void StringToPaddedBytes_WithWideCharacters_ManglesWideCharacters()
		{
			// 35 bytes long originally; 17 characters
			var testData = "中文繁體 Pусский язык";

			var bytes = Algorithms.StringToMangledAndPaddedBytes(testData);

			// Output is one byte per character plus padding.
			Assert.AreEqual(20, bytes.Length);

			// Validate against Mega JavaScript output as A32.
			var a32 = Algorithms.BytesToA32(bytes);

			Assert.AreEqual(0x6DFFDBD4, a32[0]);
			Assert.AreEqual(0x20544741, a32[1]);
			Assert.AreEqual(0x453E3C39, a32[2]);
			Assert.AreEqual(0x244F374B, a32[3]);
			Assert.AreEqual(0x3A000000, a32[4]);
		}
	}
}