namespace Mega
{
	using System;
	using System.Linq;
	using System.Reflection;
	using Newtonsoft.Json;

	/// <summary>
	/// Converts between Mega base64 strings in JSON and various data formats on the CLR.
	/// Serialization uses a conversion operator to byte[]. Deserialization uses a ctor accepting one byte[] parameter.
	/// </summary>
	internal sealed class Base64JsonConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			// We need an implicit or exlplicit conversion operator to byte[] in order to serialize.
			var type = value.GetType();

			var conversionOperator = GetSerializationOperator(type);

			if (conversionOperator == null)
				throw new InvalidOperationException("The type given to the converter is not convertible to byte[].");

			var bytes = (byte[])conversionOperator.Invoke(null, new[] { value });
			var encoded = Algorithms.Base64Encode(bytes);

			writer.WriteValue(encoded);
		}

		public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
		{
			if (reader.Value == null || !(reader.Value is string))
				return null;

			bool isNullable = IsNullableType(type);

			type = ResolveType(type);

			var ctor = GetDeserializationConstructor(type);

			if (ctor == null)
				throw new InvalidOperationException("The type given to the converter does not have a ctor accepting one string parameter.");

			if (isNullable && (string)reader.Value == "")
				return null;

			var decoded = Algorithms.Base64Decode((string)reader.Value);

			return Activator.CreateInstance(type, decoded);
		}

		public override bool CanConvert(Type type)
		{
			type = ResolveType(type);

			var ctor = GetDeserializationConstructor(type);
			var conversionOperator = GetSerializationOperator(type);

			return ctor != null && conversionOperator != null;
		}

		private static Type ResolveType(Type type)
		{
			if (IsNullableType(type))
				type = Nullable.GetUnderlyingType(type);

			return type;
		}

		private static bool IsNullableType(Type type)
		{
			var typeInfo = type.GetTypeInfo();
			return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		private static ConstructorInfo GetDeserializationConstructor(Type type)
		{
			return type.GetTypeInfo().DeclaredConstructors
				.FirstOrDefault(c =>
				{
					var parameters = c.GetParameters();
					if (parameters.Length != 1)
						return false;

					return parameters[0].ParameterType == typeof(byte[]);
				});
		}

		private static MethodInfo GetSerializationOperator(Type type)
		{
			return type.GetTypeInfo().DeclaredMethods
				.Where(m => m.Name == "op_Implicit" || m.Name == "op_Explicit")
				.Where(m => m.ReturnType == typeof(byte[]))
				.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters().Single().ParameterType == type);
		}
	}
}