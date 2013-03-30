namespace Mega.Tests.Algorithm
{
	using System;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public sealed class A32Tests
	{
		[TestMethod]
		public void BytesToA32_SeemsToWork()
		{
			var bytes = new byte[]
			{
				0x00, 0x01, 0x02, 0x03,
				0x04, 0x05, 0x06, 0x07,
				0x08, 0x09, 0x0A, 0x0B
			};

			var a32 = Algorithms.BytesToA32(bytes);

			Assert.AreEqual(3, a32.Length);

			Assert.AreEqual(0x00010203, a32[0]);
			Assert.AreEqual(0x04050607, a32[1]);
			Assert.AreEqual(0x08090A0B, a32[2]);
		}

		[TestMethod]
		public void BytesToA32_WithNoBytes_Works()
		{
			var bytes = new byte[0];

			var a32 = Algorithms.BytesToA32(bytes);

			Assert.AreEqual(0, a32.Length);
		}

		[TestMethod]
		public void BytesToA32_DoesNotWorkWithNonalignedBytes()
		{
			// Run through a set of lengths and make sure it only works if length is divisible by 4.

			for (int length = 0; length < 100; length++)
			{
				bool success;

				try
				{
					Algorithms.BytesToA32(new byte[length]);

					success = true;
				}
				catch (ArgumentException)
				{
					success = false;
				}

				bool expectedSuccess = length % 4 == 0;

				Assert.AreEqual(expectedSuccess, success);
			}
		}
	}
}