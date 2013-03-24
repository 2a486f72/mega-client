namespace Mega.Tests
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class DeriveAesKeyTests
	{
		[TestMethod]
		public void DeriveAesKey_FromPassword_SeemsToWork()
		{
			var key = Algorithms.DeriveAesKey("test password 1");

			var a32 = Algorithms.BytesToA32(key);

			Assert.AreEqual(-1698560572, a32[0]);
			Assert.AreEqual(1367866902, a32[1]);
			Assert.AreEqual(-1172790554, a32[2]);
			Assert.AreEqual(-735939499, a32[3]);
		}

		[TestMethod]
		public void DeriveAesKey_FromEmptyPassword_Works()
		{
			var key = Algorithms.DeriveAesKey("");

			var a32 = Algorithms.BytesToA32(key);

			// Sometimes "A32"-ish array members are not signed...
			Assert.AreEqual(unchecked((int)2479122403), a32[0]);
			Assert.AreEqual(2108737444, a32[1]);
			Assert.AreEqual(unchecked((int)3518906241), a32[2]);
			Assert.AreEqual(22203222, a32[3]);
		}

		[TestMethod]
		public void DeriveAesKey_FromLongPassword_Works()
		{
			var key = Algorithms.DeriveAesKey("5838687245896725967295867238956728975762857682576982576257698275627506273567209576289586295724682789267245672468268");

			var a32 = Algorithms.BytesToA32(key);

			Assert.AreEqual(910485999, a32[0]);
			Assert.AreEqual(-1479345185, a32[1]);
			Assert.AreEqual(667803337, a32[2]);
			Assert.AreEqual(-17590732, a32[3]);
		}

		[TestMethod]
		public void DeriveAesKey_FromWidePassword_Works()
		{
			var key = Algorithms.DeriveAesKey("中文繁體");

			var a32 = Algorithms.BytesToA32(key);

			Assert.AreEqual(1369044029, a32[0]);
			Assert.AreEqual(994962744, a32[1]);
			Assert.AreEqual(155264474, a32[2]);
			Assert.AreEqual(-1132618542, a32[3]);
		}
	}
}