namespace Mega.Tests.Algorithm
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class StringhashTests
	{
		[TestMethod]
		public void Stringhash_SeemsToWork()
		{
			const string testInput = "1234567890123456789";
			var expected = Algorithms.Base64Decode("rGfJJqR1_zs");

			var hash = Algorithms.Stringhash(testInput, new byte[16]);

			CollectionAssert.AreEqual(expected, hash);
		}

		[TestMethod]
		public void StringHash_WithVariousLengths_Works()
		{
			AssertHashMatch("", "7vhh3hPbXCU");
			AssertHashMatch("?", "VPjbN9gfWCw");
			AssertHashMatch("what you lookin at", "bkQr5b1WM8I");
			AssertHashMatch("eturn c===0},g:function(a,b,c,d){var e;e=0;if(d===undefined)d=[];for(;b>=32;b-=32){d.push(c);c=0}i", "hKBwNpTKRLo");

			AssertHashMatch("tääytennöö", "RQWEy97Q35c");
			AssertHashMatch("中文繁體 Pусский язык", "YiPTQhIb55s");
		}

		private static void AssertHashMatch(string s, string hashBase64)
		{
			var hash = Algorithms.Stringhash(s, new byte[16]);

			var expected = Algorithms.Base64Decode(hashBase64);

			CollectionAssert.AreEqual(expected, hash, s);
		}
	}
}