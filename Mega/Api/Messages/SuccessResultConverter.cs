namespace Mega.Api.Messages
{
	using System;
	using Newtonsoft.Json;

	internal sealed class SuccessResultConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotSupportedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (objectType != typeof(SuccessResult))
				throw new InvalidOperationException();

			if (reader.TokenType != JsonToken.Integer)
				throw new InvalidOperationException();

			var value = (long)reader.Value;

			if (value != 0)
				throw new InvalidOperationException();

			return new SuccessResult();
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(SuccessResult);
		}
	}
}