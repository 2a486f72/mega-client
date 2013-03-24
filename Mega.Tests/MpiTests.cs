namespace Mega.Tests
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class MpiTests
	{
		[TestMethod]
		public void MpiToBytes_SeemsToWork()
		{
			var mpi = Algorithms.Base64Decode("CABt_Qp7ZODvweEk5RY9JNMXoyFfUwMnc53zbP5jB4jnwWXibLLvjc-Dv5CwQAtUYRme-vRd80--178BiWl0YSOKKhQaDQKoeOUONn3KbZVWyCtyWyQZNtASPoQfizay_Dw3yP5BKsJmDpEv47awdEZzh8IqTcTKeQbpHFL-3uL5EjIENpxMh15rJUsY9w-jq6Yax-379tq67EPMUON0aYkRQ3k1Rsp9fOL6qrgoqOPmOc0cIQgx76t6SFB9LmDySkyBhtK-vcEkdn9GwzZqc6n_Jqt9K8a-mbBv3K7eO3Pa37SDncsaxEzlyLwQ2om1-bK2QwauSQl-7QwQS1a9Ejb9");

			var expectedBytes = Algorithms.Base64Decode("bf0Ke2Tg78HhJOUWPSTTF6MhX1MDJ3Od82z-YweI58Fl4myy743Pg7-QsEALVGEZnvr0XfNPvte_AYlpdGEjiioUGg0CqHjlDjZ9ym2VVsgrclskGTbQEj6EH4s2svw8N8j-QSrCZg6RL-O2sHRGc4fCKk3EynkG6RxS_t7i-RIyBDacTIdeayVLGPcPo6umGsft-_bauuxDzFDjdGmJEUN5NUbKfXzi-qq4KKjj5jnNHCEIMe-rekhQfS5g8kpMgYbSvr3BJHZ_RsM2anOp_yarfSvGvpmwb9yu3jtz2t-0g53LGsRM5ci8ENqJtfmytkMGrkkJfu0MEEtWvRI2_Q");

			var bytes = Algorithms.MpiToBytes(mpi);

			Assert.AreEqual(256, bytes.Length);
			CollectionAssert.AreEqual(expectedBytes, bytes);
		}

		[TestMethod]
		public void MpiToBytes_WithZeroLengthNumber_Works()
		{
			var mpi = new byte[2];

			var bytes = Algorithms.MpiToBytes(mpi);

			Assert.AreEqual(bytes.Length, 0);
		}
	}
}