namespace Mega.Tests
{
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Useful;

	internal static class TestHelper
	{
		private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

		public static byte[] GetRandomBytes(int count)
		{
			Argument.ValidateRange(count, "count", 0, int.MaxValue);

			var bytes = new byte[count];
			Random.GetBytes(bytes);

			return bytes;
		}

		public static void AssertStreamsAreEqual(Stream a, Stream b)
		{
			Assert.AreEqual(a.Length, b.Length);

			byte[] bufferA = new byte[1024 * 8];
			byte[] bufferB = new byte[1024 * 8];

			while (a.Position != a.Length)
			{
				var aLen = FillBufferWithData(bufferA, a);
				var bLen = FillBufferWithData(bufferB, b);

				Assert.AreEqual(aLen, bLen);

				CollectionAssert.AreEqual(bufferA, bufferB);
			}
		}

		private static int FillBufferWithData(byte[] buffer, Stream from)
		{
			int length = (int)Math.Min(buffer.Length, from.Length - from.Position);

			int bytesRead = 0;

			while (bytesRead < length)
				bytesRead += from.Read(buffer, bytesRead, length - bytesRead);

			return length;
		}
	}
}