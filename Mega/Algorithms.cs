namespace Mega
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Numerics;
	using System.Security.Cryptography;
	using Useful;

	/// <summary>
	/// Static class that contains all the Mega-specific data processing algorithms we might need to use.
	/// </summary>
	/// <remarks>
	/// ## Base64 ##
	/// Mega uses its own derivative, with the following changes:
	/// * No padding
	/// * + becomes -
	/// * / becomes _
	/// 
	/// ## A32 ##
	/// Mega uses a funny array-of-big-endian-signed-int32 data type internally. We do not use it, but we support A32 conversion
	/// from byte array since this makes it easier to test against Mega JavaScript functionality by comparing the A32 values.
	/// 
	/// ## BigInteger storage in MPI ##
	/// We use the BigInteger class to handle the MPI values. There are some things to keep in mind here:
	/// * BigInteger is signed; MPI is unsigned. Make sure to pad the data to protect the sign bit when creating BigIntegers.
	/// * BigInteger uses little-endian representation, MPI uses big-endian.
	/// </remarks>
	public static class Algorithms
	{
		#region Base64 (Mega variant)
		public static string Base64Encode(byte[] data)
		{
			Argument.ValidateIsNotNull(data, "data");

			return Convert.ToBase64String(data)
				.Replace('+', '-')
				.Replace('/', '_')
				.Replace("=", "");
		}

		/// <exception cref="FormatException">Thrown if the input data is not a valid Mega base64 string.</exception>
		public static byte[] Base64Decode(string base64)
		{
			Argument.ValidateIsNotNull(base64, "base64");

			base64 = base64
				.Replace('-', '+')
				.Replace('_', '/');

			// Must add back padding since .NET base64 format requires it.
			while (base64.Length % 4 != 0)
				base64 += "=";

			return Convert.FromBase64String(base64);
		}
		#endregion

		#region Multi-Precision Integer
		/// <summary>
		/// Reads the integer stored in an MPI structure into a byte array (big-endian, no padding, unsigned).
		/// </summary>
		/// <remarks>
		/// Funny format but not too crazy. Basically, it consists of:
		/// Length (ushort) + Data (...)
		/// 
		/// Where the length is the number of Data in bits(!). Just round up to nearest byte to get number of bytes.
		/// The data is stored in big-endian byte order (so reverse it to feed into BigInteger). The data is unsigned
		/// and does not include any padding, so you might have to extend it with a zero byte to get rid of the sign bit,
		/// since BigInteger is signed. Use the BigInteger reading/writing algorithms in this class for easy read/write.
		/// </remarks>
		public static byte[] MpiToBytes(byte[] mpi)
		{
			Argument.ValidateIsNotNull(mpi, "mpi");

			if (mpi.Length < 2)
				throw new ArgumentException("MPI too short - must be at least 2 bytes.", "mpi");

			// First 2 bytes are the big-endian encoded length in bits of the data we want to return.

			var numberOfBits = (mpi[0] << 8) + mpi[1];

			// Round up to nearest byte.
			var numberOfBytes = (numberOfBits + 7) / 8;

			var expectedLength = 2 + numberOfBytes;

			if (mpi.Length < expectedLength)
				throw new ArgumentException(string.Format("MPI too short - expected {0} bytes but only got {1}.", expectedLength, mpi.Length), "mpi");

			var result = new byte[numberOfBytes];
			Array.Copy(mpi, 2, result, 0, numberOfBytes);

			return result;
		}
		#endregion

		#region A32
		/// <summary>
		/// Transforms a little-endian byte array to the A32 format, for easy debugging and JavaScript comparison.
		/// </summary>
		public static unsafe int[] BytesToA32(byte[] bytes)
		{
			Argument.ValidateIsNotNull(bytes, "bytes");

			// Maybe A32 also supports padding for these cases? All examples I have seen so far are multiples of 4 bytes, at least.
			if (bytes.Length % 4 != 0)
				throw new ArgumentException("Byte array length is not divisible by 4 - cannot convert to A32.");

			var a32 = new int[bytes.Length / 4];

			fixed (int* dwordPtr = a32)
			{
				byte* bytePtr = (byte*)dwordPtr;

				for (int dwordIndex = 0; dwordIndex < a32.Length; dwordIndex++)
				{
					for (int inputByteOffset = 0; inputByteOffset < 4; inputByteOffset++)
					{
						var commonOffset = dwordIndex * 4;
						var outputByteOffset = (4 - inputByteOffset) - 1;

						var inputByteIndex = commonOffset + inputByteOffset;
						var outputByteIndex = commonOffset + outputByteOffset;

						*(bytePtr + outputByteIndex) = bytes[inputByteIndex];
					}
				}
			}

			return a32;
		}
		#endregion

		#region Strings
		/// <summary>
		/// Extracts the bytes of a string, with zero byte padding. String is mangled during processing.
		/// </summary>
		/// <remarks>
		/// Wide characters are written to their starting byte but may also extend out of it to the next(!) bytes,
		/// but only within their 4-byte section! Confused? You should be! Wtf?!
		/// 
		/// The author of this algorithm probably did not even consider non-ASCII strings.
		/// </remarks>
		public static unsafe byte[] StringToMangledAndPaddedBytes(string s)
		{
			Argument.ValidateIsNotNull(s, "s");

			var paddedCharacterCount = ((s.Length + 3) / 4) * 4;

			byte[] result = new byte[paddedCharacterCount];

			fixed (byte* bytePtr = result)
			{
				int* dwordPtr = (int*)bytePtr;

				for (int i = 0; i < s.Length; i++)
				{
					var dwordIndex = i / 4;
					var shift = 24 - (i % 4) * 8;

					*(dwordPtr + dwordIndex) |= unchecked(s[i] << shift);

					if ((i + 1) % 4 == 0 || i == s.Length - 1)
					{
						// We finished a dword. It must be stored big-endian, so reverse it now.
						InPlaceReverse((byte*)(dwordPtr + dwordIndex), 4);
					}
				}
			}

			return result;
		}
		#endregion

		#region Crypto
		/// <summary>
		/// AES engine configured for working with key encryption/decryption.
		/// Pass your key to CreateDecryptor(), do not use the .Key property.
		/// </summary>
		public static readonly AesManaged AesForKeys = new AesManaged
		{
			BlockSize = 128,
			KeySize = 128,
			Padding = PaddingMode.None,
			Mode = CipherMode.ECB
		};

		/// <summary>
		/// AES engine configured for working with node attribute encryption/decryption.
		/// Pass your key to CreateDecryptor(), do not use the .Key property.
		/// </summary>
		public static readonly AesManaged AesForNodeAttributes = new AesManaged
		{
			BlockSize = 128,
			KeySize = 128,
			Padding = PaddingMode.Zeros, // Note: data is not automatically unpadded.
			Mode = CipherMode.CBC
		};

		/// <summary>
		/// AES engine configured for working with node data encryption/decryption.
		/// Pass your key to CreateDecryptor(), do not use the .Key property.
		/// </summary>
		public static readonly AesManaged AesForNodeData = new AesManaged
		{
			BlockSize = 128,
			KeySize = 128,
			Padding = PaddingMode.None,
			Mode = CipherMode.ECB
		};

		/// <summary>
		/// Just an empty IV you can give to AES when it needs one.
		/// </summary>
		public static readonly byte[] EmptyAesIV = new byte[16];

		/// <summary>
		/// Hashes a string using the standard string hashing function used by Mega.
		/// </summary>
		public static unsafe byte[] Stringhash(string data, byte[] aesKey)
		{
			Argument.ValidateIsNotNull(data, "data");
			Argument.ValidateIsNotNull(aesKey, "aesKey");

			Argument.ValidateLength(aesKey, "aesKey", 16);

			byte[] stringBytes = StringToMangledAndPaddedBytes(data);
			byte[] hash = new byte[16];

			// First, mash the string into the 16 hash bytes, just cycling through and XORing.
			fixed (byte* stringPtr = stringBytes)
			fixed (byte* hashPtr = hash)
			{
				for (int i = 0; i < stringBytes.Length; i++)
					hashPtr[i % 16] ^= stringPtr[i];
			}

			// Then run it through AES for a few times.
			using (var encryptor = AesForKeys.CreateEncryptor(aesKey, EmptyAesIV))
			{
				// We use two buffers we switch around, to avoid memory allocations in the loop.
				var hashIn = hash;
				var hashOut = new byte[16];

				for (int i = 0; i < 16384; i++)
				{
					encryptor.TransformBlock(hashIn, 0, 16, hashOut, 0);

					Swap(ref hashIn, ref hashOut);
				}

				hash = hashIn;
			}

			// And pick bytes 0-3 and 8-11 out of the whole thing, giving us a final hash 8 bytes long.
			byte[] finalHash = new byte[8];
			Array.Copy(hash, 0, finalHash, 0, 4);
			Array.Copy(hash, 8, finalHash, 4, 4);

			return finalHash;
		}

		/// <summary>
		/// Turns a password into an AES key.
		/// </summary>
		public static byte[] DeriveAesKey(string password)
		{
			Argument.ValidateIsNotNull(password, "password");

			return DeriveAesKey(StringToMangledAndPaddedBytes(password));
		}

		/// <summary>
		/// Turns a set of bytes into an AES key.
		/// </summary>
		public static byte[] DeriveAesKey(byte[] keydata)
		{
			Argument.ValidateIsNotNull(keydata, "keydata");

			if (keydata.Length % 4 != 0)
				throw new ArgumentException("Length of keydata must be divisible by 4.", "keydata");

			var keydataDwordCount = keydata.Length / 4;

			// We switch around two buffers to avoid memory allocation in the loop.
			var key = new byte[]
			{
				0x93, 0xc4, 0x67, 0xe3,
				0x7d, 0xb0, 0xc7, 0xa4,
				0xd1, 0xbe, 0x3f, 0x81,
				0x01, 0x52, 0xcb, 0x56
			};
			var key2 = new byte[16];

			var subroundKey = new byte[16];

			for (int round = 0; round < 65536; round++)
			{
				// Defect?! We count dwords but we add 4 on every iteration...
				for (int keydataDwordIndex = 0; keydataDwordIndex < keydataDwordCount; keydataDwordIndex += 4)
				{
					// Reset key from last dword.
					for (int i = 0; i < subroundKey.Length; i++)
						subroundKey[i] = 0;

					for (int keyDwordIndex = 0; keyDwordIndex < 4; keyDwordIndex++)
						if (keyDwordIndex + keydataDwordIndex < keydataDwordCount)
							Array.Copy(keydata, keyDwordIndex * 4 + keydataDwordIndex * 4, subroundKey, keyDwordIndex * 4, 4);

					// AesManaged seems to cache the key, so we specify it explicitly.
					using (var encryptor = AesForKeys.CreateEncryptor(subroundKey, EmptyAesIV))
						encryptor.TransformBlock(key, 0, 16, key2, 0);

					Swap(ref key, ref key2);
				}
			}

			return key;
		}

		/// <summary>
		/// Decrypts an encrypted key. This could be either an AES key or an RSA key or really any piece of data.
		/// </summary>
		public static byte[] AesDecryptKey(byte[] encryptedKey, byte[] aesKey)
		{
			Argument.ValidateIsNotNull(encryptedKey, "encryptedKey");
			Argument.ValidateIsNotNull(aesKey, "aesKey");

			Argument.ValidateLength(aesKey, "aesKey", 16);

			if (encryptedKey.Length % 16 != 0)
				throw new ArgumentException("Encrypted key length was not divisible by 16 - this is abnormal. Are you sure you used the right input data? Did you forget to unpack an MPI, perhaps?", "encryptedKey");

			var result = new byte[encryptedKey.Length];

			using (var decryptor = AesForKeys.CreateDecryptor(aesKey, EmptyAesIV))
			{
				for (int i = 0; i < encryptedKey.Length; i += 16)
					decryptor.TransformBlock(encryptedKey, i, 16, result, i);
			}

			return result;
		}

		/// <summary>
		/// Encrypts a key. This could be either an AES key or an RSA key or really any piece of data.
		/// </summary>
		public static byte[] EncryptKey(byte[] keyToEncrypt, byte[] aesKey)
		{
			Argument.ValidateIsNotNull(keyToEncrypt, "keyToEncrypt");
			Argument.ValidateIsNotNull(aesKey, "aesKey");

			Argument.ValidateLength(aesKey, "aesKey", 16);

			if (keyToEncrypt.Length % 16 != 0)
				throw new ArgumentException("To-be-encrypted key length was not divisible by 16 - this is abnormal. Are you sure you used the right input data? Did you forget to unpack an MPI, perhaps?", "keyToEncrypt");

			var result = new byte[keyToEncrypt.Length];

			using (var decryptor = AesForKeys.CreateEncryptor(aesKey, EmptyAesIV))
			{
				for (int i = 0; i < keyToEncrypt.Length; i += 16)
					decryptor.TransformBlock(keyToEncrypt, i, 16, result, i);
			}

			return result;
		}

		/// <summary>
		/// Derives the node attributes key from the node key.
		/// </summary>
		public static byte[] DeriveNodeAttributesKey(byte[] nodeKey)
		{
			Argument.ValidateIsNotNull(nodeKey, "nodeKey");

			// For folder keys, we do not need to do any derivation.
			if (nodeKey.Length == 16)
				return nodeKey;
			else if (nodeKey.Length != 32)
				throw new ArgumentException("Node key must be either 16 or 32 bytes long.", "nodeKey");

			// Just XOR both halves to get the attributes key.
			var firstHalf = nodeKey.Take(16).ToArray();
			var secondHalf = nodeKey.Skip(16).ToArray();
			InPlaceArrayXor(firstHalf, secondHalf);

			return firstHalf;
		}

		/// <summary>
		/// Derives the node data key from the node key.
		/// </summary>
		public static byte[] DeriveNodeDataKey(byte[] nodeKey)
		{
			Argument.ValidateIsNotNull(nodeKey, "nodeKey");

			if (nodeKey.Length != 32)
				throw new ArgumentException("Node data key can only be derived from node keys that are 32 bytes long.");

			return DeriveNodeAttributesKey(nodeKey);
		}

		/// <summary>
		/// Creates a node key from its constituent components.
		/// </summary>
		public static byte[] CreateNodeKey(byte[] dataKey, byte[] nonce, byte[] metaMac)
		{
			Argument.ValidateIsNotNull(dataKey, "dataKey");
			Argument.ValidateIsNotNull(nonce, "nonce");
			Argument.ValidateIsNotNull(metaMac, "metaMac");

			Argument.ValidateLength(dataKey, "dataKey", 16);
			Argument.ValidateLength(nonce, "nonce", 8);
			Argument.ValidateLength(metaMac, "metaMac", 8);

			// Layout is: dataKey + nonce + metamac.
			// XOR first half with second half to get node key.
			var firstHalf = dataKey.ToArray();
			var secondHalf = nonce.Concat(metaMac).ToArray();
			InPlaceArrayXor(firstHalf, secondHalf);

			return firstHalf.Concat(secondHalf).ToArray();
		}

		/// <summary>
		/// Decrypts attributes associated with a filesystem node. Removes zero padding bytes from the result.
		/// </summary>
		public static byte[] DecryptNodeAttributes(byte[] encryptedData, byte[] aesKey)
		{
			Argument.ValidateIsNotNull(encryptedData, "encryptedData");
			Argument.ValidateIsNotNull(aesKey, "aesKey");

			Argument.ValidateLength(aesKey, "aesKey", 16);

			if (encryptedData.Length % 16 != 0)
				throw new ArgumentException("Encrypted data length was not divisible by 16 - this is abnormal. Are you sure you used the right input data?", "encryptedData");

			byte[] result;

			using (var decryptor = AesForNodeAttributes.CreateDecryptor(aesKey, EmptyAesIV))
				result = decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

			// Calculate padding.
			int paddingSize = 0;

			for (int i = result.Length - 1; i >= 0; i--)
			{
				if (result[i] != 0)
					break;

				paddingSize++;
			}

			// Remove padding.
			var withoutPadding = result.Take(result.Length - paddingSize).ToArray();

			return withoutPadding;
		}

		/// <summary>
		/// Encrypts attributes associated with a filesystem node. Adds zero padding bytes as needed.
		/// </summary>
		public static byte[] EncryptNodeAttributes(byte[] attributesBytes, byte[] aesKey)
		{
			Argument.ValidateIsNotNull(attributesBytes, "attributesBytes");
			Argument.ValidateIsNotNull(aesKey, "aesKey");

			Argument.ValidateLength(aesKey, "aesKey", 16);

			using (var encryptor = AesForNodeAttributes.CreateEncryptor(aesKey, EmptyAesIV))
				return encryptor.TransformFinalBlock(attributesBytes, 0, attributesBytes.Length);
		}

		public static byte[] CalculateMetaMac(byte[][] chunkMacs, byte[] fileDataKey)
		{
			Argument.ValidateIsNotNull(chunkMacs, "chunkMacs");

			if (chunkMacs.Any(mac => mac == null || mac.Length != 16))
				throw new ArgumentException("Chunk MACs were not all 16 bytes long.");

			byte[] buffer = new byte[16];
			byte[] buffer2 = new byte[16];
			var encryptor = AesForNodeData.CreateEncryptor(fileDataKey, EmptyAesIV);

			for (int i = 0; i < chunkMacs.Length; i++)
			{
				InPlaceArrayXor(buffer, chunkMacs[i]);

				encryptor.TransformBlock(buffer, 0, 16, buffer2, 0);
				Swap(ref buffer, ref buffer2);
			}

			// Final result is 0-3 XOR 4-7 and 8-11 XOR 12-15
			byte[] a = buffer.Take(4).Concat(buffer.Skip(8).Take(4)).ToArray();
			byte[] b = buffer.Skip(4).Take(4).Concat(buffer.Skip(12)).ToArray();

			InPlaceArrayXor(a, b);

			return a;
		}

		/// <summary>
		/// Decrypts a single chunk of node data (in-place) and calculates the MAC for the decrypted piece.
		/// </summary>
		/// <param name="data">Encrypted node data.</param>
		/// <param name="aesKey">Node data AES key.</param>
		/// <param name="nonce">Nonce from the main node key.</param>
		/// <param name="mac">Set to the MAC calculated during decryption.</param>
		/// <param name="offset">If the provided data does not start at the beginning of the node, specify the offset from start here.</param>
		public static unsafe void DecryptNodeDataChunk(byte[] data, byte[] aesKey, byte[] nonce, out byte[] mac, long offset = 0)
		{
			Argument.ValidateIsNotNull(data, "data");
			Argument.ValidateIsNotNull(aesKey, "aesKey");
			Argument.ValidateIsNotNull(nonce, "nonce");
			Argument.ValidateLength(aesKey, "aesKey", 16);
			Argument.ValidateLength(nonce, "nonce", 8);
			Argument.ValidateRange(offset, "offset", min: 0);

			var encryptor = AesForNodeData.CreateEncryptor(aesKey, EmptyAesIV);

			int blockCount = data.Length / 16;
			if (data.Length % 16 != 0)
				blockCount++;

			long nonceAsLong = BitConverter.ToInt64(nonce, 0);

			byte[] ctrClear = new byte[16];
			byte[] ctrEncrypted = new byte[16];
			byte[] macBuffer = new byte[16];
			byte[] macBufferTemp = new byte[16];

			fixed (byte* buffer = data)
			fixed (byte* macPtr = macBuffer)
			fixed (byte* ctrPtr = ctrClear)
			fixed (byte* ctrEncryptedPtr = ctrEncrypted)
			{
				// First 8 bytes of CTR is the nonce. It is not updated for different blocks.
				*(long*)&ctrPtr[0] = nonceAsLong;
				// Next 4 + 4 bytes are related to the offset of the chunk in the file. NB! Big-endian treatment...
				*(uint*)&ctrPtr[8] = unchecked((uint)(offset / 0x1000000000));
				InPlaceReverse(ctrPtr + 8, 4);
				*(uint*)&ctrPtr[12] = unchecked((uint)(offset / 0x10));
				InPlaceReverse(ctrPtr + 12, 4);

				// Fill MAC start value.
				*(long*)&macPtr[0] = nonceAsLong;
				*(long*)&macPtr[8] = nonceAsLong;

				for (long blockId = 0; blockId < blockCount; blockId++)
				{
					encryptor.TransformBlock(ctrClear, 0, ctrClear.Length, ctrEncrypted, 0);

					// XOR buffer with encrypted CTR. Stop when end of buffer is reached.
					if (blockId != blockCount - 1)
					{
						// Full block.
						ulong* blockData = (ulong*)&buffer[blockId * 16];
						ulong* ctr = (ulong*)&ctrEncryptedPtr[0];

						blockData[0] ^= ctr[0];
						blockData[1] ^= ctr[1];

						ulong* macPtrLong = (ulong*)macPtr;
						macPtrLong[0] ^= blockData[0];
						macPtrLong[1] ^= blockData[1];
					}
					else
					{
						// Potentially incomplete block.
						for (int relativeIndex = 0; relativeIndex < 16 && blockId * 16 + relativeIndex < data.Length; relativeIndex++)
						{
							long absoluteIndex = blockId * 16 + relativeIndex;

							buffer[absoluteIndex] = (byte)(buffer[absoluteIndex] ^ ctrEncryptedPtr[relativeIndex]);
							macPtr[relativeIndex] ^= buffer[absoluteIndex];
						}
					}

					encryptor.TransformBlock(macBuffer, 0, macBuffer.Length, macBufferTemp, 0);
					Array.Copy(macBufferTemp, macBuffer, 16);

					// Increase the two accumulators. Remember... big endian treatement!
					InPlaceReverse(ctrPtr + 8, 4);
					InPlaceReverse(ctrPtr + 12, 4);

					if (++*(uint*)(ctrPtr + 12) == 0)
						(*(uint*)(ctrPtr + 8))++;

					// ...
					InPlaceReverse(ctrPtr + 8, 4);
					InPlaceReverse(ctrPtr + 12, 4);
				}
			}

			mac = macBuffer;
		}

		/// <summary>
		/// Encrypts a single chunk of node data (in-place) and calculates the MAC for the encrypted piece.
		/// </summary>
		/// <param name="data">Plain node data.</param>
		/// <param name="aesKey">Node data AES key.</param>
		/// <param name="nonce">Nonce from the main node key.</param>
		/// <param name="mac">Set to the MAC calculated during encryption.</param>
		/// <param name="offset">If the provided data does not start at the beginning of the node, specify the offset from start here.</param>
		public static unsafe void EncryptNodeDataChunk(byte[] data, byte[] aesKey, byte[] nonce, out byte[] mac, long offset = 0)
		{
			Argument.ValidateIsNotNull(data, "data");
			Argument.ValidateIsNotNull(aesKey, "aesKey");
			Argument.ValidateIsNotNull(nonce, "nonce");
			Argument.ValidateLength(aesKey, "aesKey", 16);
			Argument.ValidateLength(nonce, "nonce", 8);
			Argument.ValidateRange(offset, "offset", min: 0);

			var encryptor = AesForNodeData.CreateEncryptor(aesKey, EmptyAesIV);

			int blockCount = data.Length / 16;
			if (data.Length % 16 != 0)
				blockCount++;

			long nonceAsLong = BitConverter.ToInt64(nonce, 0);

			byte[] ctrClear = new byte[16];
			byte[] ctrEncrypted = new byte[16];
			byte[] macBuffer = new byte[16];
			byte[] macBufferTemp = new byte[16];

			fixed (byte* buffer = data)
			fixed (byte* macPtr = macBuffer)
			fixed (byte* ctrPtr = ctrClear)
			fixed (byte* ctrEncryptedPtr = ctrEncrypted)
			{
				// First 8 bytes of CTR is the nonce. It is not updated for different blocks.
				*(long*)&ctrPtr[0] = nonceAsLong;
				// Next 4 + 4 bytes are related to the offset of the chunk in the file. NB! Big-endian treatment...
				*(uint*)&ctrPtr[8] = unchecked((uint)(offset / 0x1000000000));
				InPlaceReverse(ctrPtr + 8, 4);
				*(uint*)&ctrPtr[12] = unchecked((uint)(offset / 0x10));
				InPlaceReverse(ctrPtr + 12, 4);

				// Fill MAC start value.
				*(long*)&macPtr[0] = nonceAsLong;
				*(long*)&macPtr[8] = nonceAsLong;

				for (long blockId = 0; blockId < blockCount; blockId++)
				{
					encryptor.TransformBlock(ctrClear, 0, ctrClear.Length, ctrEncrypted, 0);

					// XOR buffer with encrypted CTR. Stop when end of buffer is reached.
					if (blockId != blockCount - 1)
					{
						// Full block.
						ulong* blockData = (ulong*)&buffer[blockId * 16];
						ulong* ctr = (ulong*)&ctrEncryptedPtr[0];

						ulong* macPtrLong = (ulong*)macPtr;
						macPtrLong[0] ^= blockData[0];
						macPtrLong[1] ^= blockData[1];

						blockData[0] ^= ctr[0];
						blockData[1] ^= ctr[1];
					}
					else
					{
						// Potentially incomplete block.
						for (int relativeIndex = 0; relativeIndex < 16 && blockId * 16 + relativeIndex < data.Length; relativeIndex++)
						{
							long absoluteIndex = blockId * 16 + relativeIndex;

							macPtr[relativeIndex] ^= buffer[absoluteIndex];
							buffer[absoluteIndex] = (byte)(buffer[absoluteIndex] ^ ctrEncryptedPtr[relativeIndex]);
						}
					}

					encryptor.TransformBlock(macBuffer, 0, macBuffer.Length, macBufferTemp, 0);
					Array.Copy(macBufferTemp, macBuffer, 16);

					// Increase the two accumulators. Remember... big endian treatement!
					InPlaceReverse(ctrPtr + 8, 4);
					InPlaceReverse(ctrPtr + 12, 4);

					if (++*(uint*)(ctrPtr + 12) == 0)
						(*(uint*)(ctrPtr + 8))++;

					// ...
					InPlaceReverse(ctrPtr + 8, 4);
					InPlaceReverse(ctrPtr + 12, 4);
				}
			}

			mac = macBuffer;
		}
		#endregion

		#region BigInteger
		/// <summary>
		/// Reads a BigInteger from an array of bytes representing an unsigned big-endian integer.
		/// </summary>
		public static BigInteger BytesToBigInteger(byte[] bigEndianBytes)
		{
			Argument.ValidateIsNotNull(bigEndianBytes, "bigEndianBytes");

			// Must add zero byte for padding if sign bit is set, since input must be treated as unsigned..
			if (bigEndianBytes[0] > 127)
				bigEndianBytes = new byte[1].Concat(bigEndianBytes).ToArray();

			var littleEndian = bigEndianBytes.Reverse().ToArray();

			return new BigInteger(littleEndian);
		}

		/// <summary>
		/// Transforms a BigInteger into an array of bytes representing an unsigned big-endian integer.
		/// Optionally pads the result to the specified number of bytes, if fixed-length output is desired.
		/// </summary>
		public static byte[] BigIntegerToBytes(BigInteger integer, int? expectedBytes = null)
		{
			Argument.ValidateIsNotNull(integer, "integer");

			if (expectedBytes.HasValue)
				Argument.ValidateRange(expectedBytes.Value, "expectedBytes", 0, int.MaxValue);

			if (integer < BigInteger.Zero)
				throw new ArgumentOutOfRangeException("integer", "The BigInteger cannot be negative.");

			// Little-endian, can have one byte of padding to protect sign bit.
			var littleEndianBytes = integer.ToByteArray();

			// Get rid of built-in padding byte if it exists.
			if (littleEndianBytes[littleEndianBytes.Length - 1] == 0)
				littleEndianBytes = littleEndianBytes.Take(littleEndianBytes.Length - 1).ToArray();

			if (expectedBytes.HasValue)
			{
				if (littleEndianBytes.Length > expectedBytes.Value)
				{
					throw new ArgumentException("BigInteger does not fit into expected number of output bytes.", "integer");
				}
				else if (littleEndianBytes.Length < expectedBytes.Value)
				{
					// Add padding.

					var paddingBytes = expectedBytes.Value - littleEndianBytes.Length;

					littleEndianBytes = littleEndianBytes.Concat(new byte[paddingBytes]).ToArray();
				}
			}

			var bigEndianBytes = littleEndianBytes.Reverse().ToArray();

			return bigEndianBytes;
		}
		#endregion

		#region RSA key management
		/// <summary>
		/// E component of the RSA keys used by Mega.
		/// </summary>
		public static readonly BigInteger RsaE = new BigInteger(17);

		/// <summary>
		/// Only includes the values we are actually interested in for the purposes of this library.
		/// </summary>
		public sealed class RsaPrivateKey
		{
			/// <summary>
			/// Prime 1.
			/// </summary>
			public BigInteger P { get; private set; }

			/// <summary>
			/// Prime 2.
			/// </summary>
			public BigInteger Q { get; private set; }

			/// <summary>
			/// Private exponent.
			/// </summary>
			public BigInteger D { get; private set; }

			/// <summary>
			/// Modulus (P * Q).
			/// </summary>
			public BigInteger N { get; private set; }

			public RsaPrivateKey(BigInteger p, BigInteger q, BigInteger d)
			{
				P = p;
				Q = q;
				D = d;

				N = BigInteger.Multiply(p, q);
			}
		}

		/// <summary>
		/// Transforms an array of MPIs into the RSA private key structure used by this library.
		/// </summary>
		public static RsaPrivateKey MpiArrayBytesToRsaPrivateKey(byte[] mpiArrayBytes)
		{
			Argument.ValidateIsNotNull(mpiArrayBytes, "mpiArrayBytes");

			List<BigInteger> components = new List<BigInteger>();

			// Expected components: p q d u
			// u is InverseP, which is really weird because normally InverseQ is used, but maybe there is a mathematical reason.
			// We do not use u because it makes all the nice standard algorithms not work, so it is weird.

			for (int i = 0; i < 4; i++)
			{
				var integerBytes = MpiToBytes(mpiArrayBytes);
				var integer = BytesToBigInteger(integerBytes);

				components.Add(integer);

				mpiArrayBytes = mpiArrayBytes.Skip(2 + integerBytes.Length).ToArray();
			}

			if (mpiArrayBytes.Length >= 16)
				throw new ArgumentException("There was too much data left over in the MPI array after reading.", "mpiArrayBytes");

			if (components.Count != 4)
				throw new ArgumentException("Could not extract the 4 private key components from MPI array.", "mpiArrayBytes");

			return new RsaPrivateKey(components[0], components[1], components[2]);
		}

		/// <summary>
		/// Only includes the values we are actually interested in for the purposes of this library.
		/// </summary>
		public sealed class RsaPublicKey
		{
			/// <summary>
			/// Public exponent.
			/// </summary>
			public BigInteger E { get; private set; }

			/// <summary>
			/// Modulus (P * Q)
			/// </summary>
			public BigInteger N { get; private set; }

			public RsaPublicKey(BigInteger n, BigInteger e)
			{
				if (e != RsaE)
					throw new ArgumentException(string.Format("Expected e == 17 but got {0} instead. The crypto in this library assumes e == 17.", e), "e");

				E = e;
				N = n;
			}
		}

		/// <summary>
		/// Transforms an array of MPIs into the RSA public key structure used by this library.
		/// </summary>
		public static RsaPublicKey MpiArrayBytesToRsaPublicKey(byte[] mpiArrayBytes)
		{
			Argument.ValidateIsNotNull(mpiArrayBytes, "mpiArrayBytes");

			List<BigInteger> components = new List<BigInteger>();

			// Expected components: n e
			for (int i = 0; i < 2; i++)
			{
				var integerBytes = MpiToBytes(mpiArrayBytes);
				var integer = BytesToBigInteger(integerBytes);

				components.Add(integer);

				mpiArrayBytes = mpiArrayBytes.Skip(2 + integerBytes.Length).ToArray();
			}

			if (mpiArrayBytes.Length >= 16)
				throw new ArgumentException("There was too much data left over in the MPI array after reading.", "mpiArrayBytes");

			if (components.Count != 2)
				throw new ArgumentException("Could not extract the 2 public key components from MPI array.", "mpiArrayBytes");

			return new RsaPublicKey(components[0], components[1]);
		}
		#endregion

		#region RSA encryption/decryption
		public static byte[] RsaDecrypt(byte[] encryptedData, RsaPrivateKey key)
		{
			Argument.ValidateIsNotNull(encryptedData, "encryptedData");
			Argument.ValidateIsNotNull(key, "key");

			// RSA works with integers, not bytes, so transform the bytes into an integer.

			var encryptedInteger = BytesToBigInteger(encryptedData);

			// It's as simple as that: s = (c**d) mod n
			var decryptedInteger = BigInteger.ModPow(encryptedInteger, key.D, key.N);

			return BigIntegerToBytes(decryptedInteger);
		}

		public static byte[] RsaEncrypt(byte[] clearData, RsaPublicKey key)
		{
			Argument.ValidateIsNotNull(clearData, "clearData");
			Argument.ValidateIsNotNull(key, "key");

			// RSA works with integers, not bytes, so transform the bytes into an integer.

			var clearInteger = BytesToBigInteger(clearData);

			// It's as simple as that: c = (s**e) mod n
			var encryptedInteger = BigInteger.ModPow(clearInteger, key.E, key.N);

			return BigIntegerToBytes(encryptedInteger);
		}
		#endregion

		#region Content chunking
		/// <summary>
		/// Returns the chunk sizes, provided the total size.
		/// </summary>
		public static int[] MeasureChunks(long totalSize)
		{
			Argument.ValidateRange(totalSize, "totalSize", 0, long.MaxValue);

			List<int> chunks = new List<int>();
			long remainingSize = totalSize;

			// First add the chunks from the fixed sizes list.
			for (int i = 0; i < FixedChunkSizes.Length; i++)
			{
				int size = (int)Math.Min(remainingSize, FixedChunkSizes[i]);

				chunks.Add(size);
				remainingSize -= size;

				if (remainingSize == 0)
					break;
			}

			// Then in 1 MB intervals to end of file.
			while (remainingSize != 0)
			{
				int size = (int)Math.Min(remainingSize, 1024 * 1024);

				chunks.Add(size);
				remainingSize -= size;
			}

			return chunks.ToArray();
		}

		private static readonly int[] FixedChunkSizes = new[]
		{
			128 * 1024,
			256 * 1024,
			384 * 1024,
			512 * 1024,
			640 * 1024,
			768 * 1024,
			896 * 1024,
			1024 * 1024,
		};
		#endregion

		#region Random numbers
		private static readonly RandomNumberGenerator _random = new RNGCryptoServiceProvider();

		public static byte[] GetRandomBytes(int length)
		{
			var bytes = new byte[length];
			_random.GetBytes(bytes);

			return bytes;
		}
		#endregion

		#region Internal stuff
		private static void Swap(ref byte[] a, ref byte[] b)
		{
			var temp = a;
			a = b;
			b = temp;
		}

		private static unsafe void InPlaceReverse(byte* ptr, int length)
		{
			for (int i = 0; i < length / 2; i++)
			{
				int j = length - i - 1;

				ptr[i] ^= ptr[j];
				ptr[j] ^= ptr[i];
				ptr[i] ^= ptr[j];
			}
		}

		private static void InPlaceArrayXor(byte[] to, byte[] from)
		{
			for (int i = 0; i < to.Length; i++)
				to[i] ^= from[i];
		}
		#endregion
	}
}