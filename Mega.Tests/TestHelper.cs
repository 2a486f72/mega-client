namespace Mega.Tests
{
	using System.Security.Cryptography;
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
	}
}