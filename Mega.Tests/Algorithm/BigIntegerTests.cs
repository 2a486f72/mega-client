namespace Mega.Tests.Algorithm
{
	using System;
	using System.Numerics;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class BigIntegerTests
	{
		[TestMethod]
		public void BigInteger_ImportAndExport_SeemsToWork()
		{
			byte[] testData = Convert.FromBase64String("1AkMwy3SPbJtL/k2RUPNztBQKow0NX9LVr5/73+zR3cuwgUToYkVefKdzlTgeri9CAVUq/+jU6o+P7sUpPUN+V97quZa00m3GSIdonRMdaMrDDH5aHnkQgOsCjLJDWXU6+TQBqLumR3XMSat3VO09Dps+6NcMc+uMi5atC3tb+0=");

			var integer = Algorithms.BytesToBigInteger(testData);
			var outputData = Algorithms.BigIntegerToBytes(integer, 128);

			CollectionAssert.AreEqual(testData, outputData);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void NegativeBigInteger_ExportFails()
		{
			Algorithms.BigIntegerToBytes(new BigInteger(-5));
		}

		[TestMethod]
		public void PaddingIsAdded_OnBigIntegerExport()
		{
			var bytes = Algorithms.BigIntegerToBytes(new BigInteger(1), 128);

			Assert.AreEqual(128, bytes.Length);

			Assert.AreEqual(1, bytes[bytes.Length - 1]);

			for (int i = 0; i < bytes.Length - 1; i++)
				Assert.AreEqual(0, bytes[i]);
		}

		[TestMethod]
		public void SignBitPadding_IsRemovedOnExport()
		{
			var bytes = Algorithms.BigIntegerToBytes(new BigInteger(255));

			Assert.AreEqual(1, bytes.Length);
			Assert.AreEqual(255, bytes[0]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ExceptionIsThrown_IfDataDoesNotFit()
		{
			Algorithms.BigIntegerToBytes(new BigInteger(256), 1);
		}
	}
}