namespace Mega.Cryptography
{
	using System.Linq;

	public static class ExtensionsForAesFastEngine
	{
		/// <summary>
		/// Processes a buffer consisting of multiple blocks.
		/// </summary>
		public static byte[] ProcessBuffer(this AesFastEngine instance, byte[] buffer)
		{
			byte[] result = new byte[buffer.Length];

			for (int i = 0; i < buffer.Length; i += 16)
				instance.ProcessBlock(buffer, i, result, i);

			return result;
		}

		/// <summary>
		/// Processes a buffer consisting of multiple blocks, adds zero padding as needed.
		/// </summary>
		public static byte[] ProcessBufferAddPadding(this AesFastEngine instance, byte[] buffer)
		{
			int paddingBytes = 16 - (buffer.Length % 16);
			if (paddingBytes == 16)
				paddingBytes = 0;

			byte[] result = new byte[buffer.Length + paddingBytes];

			for (int i = 0; i < buffer.Length; i += 16)
			{
				if (buffer.Length - 16 < i)
				{
					// Last block, needs padding added.
					var paddedBlock = buffer.Skip(i).Concat(new byte[paddingBytes]).ToArray();

					instance.ProcessBlock(paddedBlock, i, result, i);
				}
				else
				{
					instance.ProcessBlock(buffer, i, result, i);
				}
			}

			return result;
		}
	}
}