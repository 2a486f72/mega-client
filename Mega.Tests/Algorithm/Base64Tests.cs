namespace Mega.Tests.Algorithm
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class Base64Tests
	{
		[TestMethod]
		public void RoundtripEncoding_SeemsToWork()
		{
			var testData = TestHelper.GetRandomBytes(4096);

			var encoded = Algorithms.Base64Encode(testData);
			var decoded = Algorithms.Base64Decode(encoded);

			CollectionAssert.AreEqual(testData, decoded);
		}

		[TestMethod]
		public void EncodedData_DoesNotContainBadCharacters()
		{
			var testData = TestHelper.GetRandomBytes(4096);

			var encoded = Algorithms.Base64Encode(testData);

			// Make sure we do not have any "bad" characters from standard base64.
			Assert.IsFalse(encoded.Contains("="));
			Assert.IsFalse(encoded.Contains("/"));
			Assert.IsFalse(encoded.Contains("+"));
		}

		[TestMethod]
		public void EmptyData_RoundtripWorksFine()
		{
			var testData = new byte[0];

			var encoded = Algorithms.Base64Encode(testData);
			var decoded = Algorithms.Base64Decode(encoded);

			CollectionAssert.AreEqual(testData, decoded);
		}

		[TestMethod]
		public void VariousLengths_WorkFine()
		{
			for (int i = 0; i < 100; i++)
			{
				var testData = TestHelper.GetRandomBytes(i);

				var encoded = Algorithms.Base64Encode(testData);
				var decoded = Algorithms.Base64Decode(encoded);

				CollectionAssert.AreEqual(testData, decoded);
			}
		}
	}
}