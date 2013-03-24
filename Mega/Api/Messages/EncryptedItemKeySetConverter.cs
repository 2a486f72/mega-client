namespace Mega.Api.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;

	internal sealed class EncryptedItemKeySetConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			if (!(value is EncryptedItemKey[]))
				throw new ArgumentException();

			writer.WriteValue(string.Join("/", ((EncryptedItemKey[])value).Select(nk => string.Format("{0}:{1}", nk.SourceID, nk.EncryptedKey))));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			// Value is list of EncryptedItemKeys separated by /

			var value = reader.Value.ToString().Trim('/');

			var keys = new List<EncryptedItemKey>();

			while (value.Length != 0)
			{
				var nextSlashIndex = value.IndexOf('/');

				string keyString;

				if (nextSlashIndex == -1)
					keyString = value;
				else
					keyString = value.Substring(0, nextSlashIndex);

				// EncryptedItemKey contents are SourceID:EncryptedKey
				var separatorIndex = keyString.IndexOf(':');

				if (separatorIndex == -1)
					throw new NotSupportedException("Did not find source ID and key separator in keystring.");

				keys.Add(new EncryptedItemKey(keyString.Substring(0, separatorIndex), keyString.Substring(separatorIndex + 1)));

				value = value.Substring(keyString.Length).Trim('/');
			}

			return keys.ToArray();
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(EncryptedItemKey[]);
		}
	}
}